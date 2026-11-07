using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OPNX.Lib.Common.Primitives.Media;
using OPNX.Lib.Streaming.RTSP;
using OPNX.Lib.Streaming.RTSP.Commons;
using OPNX.Lib.Streaming.RTSP.Commons.Interfaces;
using OPNX.Lib.Streaming.RTSP.Generic;
using OPNX.Lib.Streaming.RTSP.RTP;
using OPNX.Lib.Streaming.WebRTC.DataChannel;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace OPNX.Samples.WebRtcViewer;

// A loopback-only sample bridge. Credentials stay in the desktop process.
internal sealed class LocalRelay : IAsyncDisposable
{
    private readonly WebApplication _host;
    private readonly ConcurrentDictionary<Guid, Viewer> _viewers = new();
    private readonly string _token = Guid.NewGuid().ToString("N");
    private readonly Channel<VideoFrame> _frames = Channel.CreateBounded<VideoFrame>(new BoundedChannelOptions(3)
    { SingleReader = true, SingleWriter = false, FullMode = BoundedChannelFullMode.DropOldest });
    private readonly Task _sender;
    private RTSPClient? _source;
    private byte[][] _parameterSets = [];
    private long _generation;
    private volatile bool _ready;
    private bool _disposed;
    public event Action<string>? StatusChanged;
    public string BaseUrl { get; private set; } = "";
    public string PlayerUrl => BaseUrl + _token + "/";
    public bool HasSource => _source != null && _ready;
    private record VideoFrame(long Generation, uint Timestamp, bool KeyFrame, byte[][] Nals);

    private LocalRelay(WebApplication host)
    {
        _host = host;
        _sender = Task.Run(SendFramesAsync);
    }
    public static async Task<LocalRelay> CreateAsync()
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { Args = [] });
        builder.Configuration.Sources.Clear(); builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        var relay = new LocalRelay(builder.Build());
        relay._host.UseWebSockets();
        relay._host.Run(relay.HandleRequestAsync);
        try
        {
            await relay._host.StartAsync();
            relay.BaseUrl = relay._host.Urls.Single().TrimEnd('/') + "/";
            return relay;
        }
        catch { await relay.DisposeAsync(); throw; }
    }
    private async Task HandleRequestAsync(HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        string root = "/" + _token + "/";
        if (context.Request.Path == root)
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.SendFileAsync(Path.Combine(AppContext.BaseDirectory, "Player.html"));
            return;
        }
        if (context.Request.Path == root + "status")
        { await context.Response.WriteAsJsonAsync(new { ready = _ready, viewers = _viewers.Count }); return; }
        if (context.Request.Path != root + "signal" || !context.WebSockets.IsWebSocketRequest || !_ready || _viewers.Count >= 4)
        { context.Response.StatusCode = 404; return; }
        // Only this loopback player may open the signaling endpoint.
        if (!string.Equals(context.Request.Headers.Origin, BaseUrl.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
        { context.Response.StatusCode = 403; return; }
        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        using var viewer = new Viewer(socket);
        if (!_viewers.TryAdd(viewer.Id, viewer)) return;
        try
        {
            await viewer.RunAsync(context.RequestAborted);
        }
        catch (OperationCanceledException) { }
        catch (Exception) { StatusChanged?.Invoke("Browser disconnected · Reconnect from the player if needed."); }
        finally { _viewers.TryRemove(viewer.Id, out _); }
    }
    public async Task StartSourceAsync(VideoSource source, CancellationToken cancellation)
    {
        await StopSourceAsync();
        var configured = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var client = new RTSPClient(source);
        long generation = Interlocked.Increment(ref _generation);
        _source = client;
        client.StreamConfigured += (_, args) =>
        {
            if (generation != Interlocked.Read(ref _generation) || args.ChannelType != ChannelTypes.Video) return;
            if (!args.PayloadName.Equals("H264", StringComparison.OrdinalIgnoreCase))
            { configured.TrySetException(new NotSupportedException("This sample requires H.264. Select the camera's H.264 stream; H.265 transcoding is not included.")); return; }
            if (args.StreamConfigurationData is H264StreamConfigurationData data)
                _parameterSets = data.OutOfBandNal?.Select(n => n.ToArray()).ToArray() ?? [];
            configured.TrySetResult();
        };
        client.NalUnitExtracted += (_, args) =>
        {
            if (generation != Interlocked.Read(ref _generation) || args.ChannelType != ChannelTypes.Video ||
                !args.PayloadName.Equals("H264", StringComparison.OrdinalIgnoreCase)) return;
            _frames.Writer.TryWrite(new VideoFrame(generation, args.TimeStamp, args.IsKeyFrame,
                args.Data.Select(n => n.ToArray()).ToArray()));
        };
        client.Stopped += (_, args) =>
        {
            if (generation != Interlocked.Read(ref _generation)) return;
            _ready = false;
            configured.TrySetException(new IOException("RTSP stopped: " + args.StopReason));
            foreach (var viewer in _viewers.Values) viewer.Abort();
            StatusChanged?.Invoke("RTSP stopped: " + args.StopReason);
        };
        await client.StartAsync().WaitAsync(cancellation);
        await configured.Task.WaitAsync(cancellation);
        _ready = true;
        StatusChanged?.Invoke("RTSP ready · H.264 · Waiting for browser and next keyframe");
    }
    private async Task SendFramesAsync()
    {
        var maker = new RTPPacketMaker((int)VideoPayloadType.H264);
        await foreach (var frame in _frames.Reader.ReadAllAsync())
        {
            if (frame.Generation != Interlocked.Read(ref _generation)) continue;
            var viewers = _viewers.Values.Where(v => v.Peer.IsConnected && (!v.WaitingForKeyframe || frame.KeyFrame)).ToArray();
            if (viewers.Length == 0) continue;
            IEnumerable<byte[]> nals = frame.KeyFrame ? _parameterSets.Concat(frame.Nals) : frame.Nals;
            var packets = nals.SelectMany(nal => maker.GetRTPPackets(CodecId.H264, frame.Timestamp, nal)).ToArray();
            foreach (var viewer in viewers)
            {
                try
                {
                    for (int i = 0; i < packets.Length; i++)
                    {
                        var packet = new RTPPacket(packets[i]);
                        viewer.Peer.SendRtpVideoData((int)VideoPayloadType.H264, packet.Payload.Span, frame.Timestamp, i == packets.Length - 1 ? 1 : 0);
                    }
                    viewer.WaitingForKeyframe = false;
                }
                catch { viewer.Abort(); }
            }
        }
    }
    public async Task StopSourceAsync()
    {
        _ready = false; Interlocked.Increment(ref _generation);
        foreach (var viewer in _viewers.Values) viewer.Abort();
        var source = _source; _source = null;
        _parameterSets = [];
        if (source != null) await Task.Run(() => source.Stop(RTSPClientStopReason.COMMAND));
    }
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await StopSourceAsync();
        _frames.Writer.TryComplete(); await _sender;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try { await _host.StopAsync(timeout.Token); }
        finally { await _host.DisposeAsync(); }
    }

    private sealed class Viewer : IDisposable
    {
        private readonly WebSocket _socket;
        private readonly Channel<string> _messages = Channel.CreateBounded<string>(64);
        public Guid Id { get; } = Guid.NewGuid();
        public DataChannelPeerConnection Peer { get; } = new();
        public bool WaitingForKeyframe { get; set; } = true;
        public Viewer(WebSocket socket) { _socket = socket; }
        public void Abort() => _socket.Abort();
        public async Task RunAsync(CancellationToken cancellation)
        {
            using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            // Serialize all signaling writes; buffer candidates until after the SDP offer.
            var candidates = new List<string>();
            object gate = new(); bool offered = false;
            Peer.LocalCandidateCreated += (_, candidate) =>
            {
                string message = JsonSerializer.Serialize(new { candidate = candidate.Content, sdpMid = candidate.Mid });
                lock (gate)
                {
                    if (!offered) candidates.Add(message);
                    else if (!_messages.Writer.TryWrite(message)) Abort();
                }
            };
            Peer.ConnectionStateChanged += (_, state) =>
            {
                if (state is DataChannelDotnet.Bindings.rtcState.RTC_FAILED or DataChannelDotnet.Bindings.rtcState.RTC_CLOSED) Abort();
            };
            var sending = SendMessagesAsync(lifetime.Token);
            try
            {
                var offer = await Peer.CreateOfferAsync();
                await _messages.Writer.WriteAsync(offer.Sdp, lifetime.Token);
                lock (gate)
                {
                    offered = true;
                    foreach (var message in candidates) if (!_messages.Writer.TryWrite(message)) Abort();
                    candidates.Clear();
                }
                byte[] buffer = new byte[8192];
                while (_socket.State == WebSocketState.Open)
                {
                    using var message = new MemoryStream();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _socket.ReceiveAsync(buffer, lifetime.Token);
                        if (result.MessageType == WebSocketMessageType.Close) return;
                        if (result.MessageType != WebSocketMessageType.Text || message.Length + result.Count > 256 * 1024)
                            throw new InvalidDataException("Invalid signaling message.");
                        message.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);
                    string text = Encoding.UTF8.GetString(message.ToArray());
                    if (text.StartsWith("v=", StringComparison.Ordinal)) Peer.SetRemoteAnswer(text);
                    else
                    {
                        using var json = JsonDocument.Parse(text);
                        var root = json.RootElement;
                        if (root.TryGetProperty("candidate", out var candidate) && root.TryGetProperty("sdpMid", out var mid) &&
                            candidate.ValueKind == JsonValueKind.String && mid.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(candidate.GetString()))
                            Peer.AddRemoteCandidate(candidate.GetString()!, mid.GetString()!);
                    }
                }
            }
            finally
            {
                lifetime.Cancel(); _messages.Writer.TryComplete(); Abort();
                try { await sending; } catch (OperationCanceledException) { } catch (WebSocketException) { }
            }
        }
        private async Task SendMessagesAsync(CancellationToken cancellation)
        {
            try
            {
                await foreach (var message in _messages.Reader.ReadAllAsync(cancellation))
                    await _socket.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, cancellation);
            }
            catch { Abort(); throw; }
        }
        public void Dispose() { Peer.Dispose(); }
    }
}
