using OPNX.Lib.Network.Abstractions.Events;
using OPNX.Lib.Network.Protocol.Abstractions;
using OPNX.Lib.Network.Protocol.Framing;
using OPNX.Lib.Network.Protocol.NamedPipe;
using OPNX.Lib.Network.Protocol.SharedMemory;
using OPNX.Lib.Network.Protocol.Tcp;
using OPNX.Lib.Network.Transport.NamedPipe;
using OPNX.Lib.Network.Transport.SharedMemory;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace OPNX.Samples.ProtocolChat;

internal static class Program
{
    private static readonly PacketHeader TextPacketHeader = new(PacketFlags.None, PacketType.Notice, 1);

    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "/?")
        {
            PrintUsage();
            return 0;
        }
        // Preserve the old server/client syntax as shorthand for TCP.
        if (args[0] is "server" or "client")
            args = ["tcp", .. args];
        if (args.Length < 2 || args[1] is not ("server" or "client") || args[0] is not ("tcp" or "pipe" or "shm"))
        {
            PrintUsage();
            return 1;
        }
        using var shutdown = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; shutdown.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            string role = args[1];
            string address = args.Length > 2 ? args[2] : args[0] == "tcp" ? "127.0.0.1" : "opnx-protocol-chat";
            switch (args[0])
            {
                case "tcp":
                    if (!IPAddress.TryParse(address, out var host) ||
                        (args.Length > 3 && (!int.TryParse(args[3], out _) || int.Parse(args[3]) is < 1 or > 65535)))
                        throw new ArgumentException("TCP requires an IP address and a port between 1 and 65535.");
                    int port = args.Length > 3 ? int.Parse(args[3]) : 9000;
                    await RunTcpAsync(role, host, port, shutdown.Token);
                    break;
                case "pipe":
                    await RunPipeAsync(role, address, shutdown.Token);
                    break;
                case "shm":
                    if (!OperatingSystem.IsWindows())
                        throw new PlatformNotSupportedException("This SDK SharedMemory implementation requires Windows.");
                    await RunSharedMemoryAsync(role, address, shutdown.Token);
                    break;
            }
            return 0;
        }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested) { return 0; }
        catch (Exception ex) when (ex is SocketException or IOException or TimeoutException or ArgumentException or PlatformNotSupportedException or UnauthorizedAccessException)
        {
            Console.WriteLine($"Connection failed: {ex.Message}");
            return 1;
        }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private static async Task RunTcpAsync(string role, IPAddress host, int port, CancellationToken cancellationToken)
    {
        if (role == "client")
        {
            using var socket = new TcpClient();
            await socket.ConnectAsync(host, port, cancellationToken);
            await using var client = new OPNXTcpClient(socket);
            await RunProtocolClientAsync(client, role, cancellationToken);
            return;
        }
        await using var server = new OPNXTcpServer(host.ToString(), port);
        var accepted = new TaskCompletionSource<TcpClient>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Accept(object? sender, ClientAcceptedEventArgs e)
        {
            if (!accepted.TrySetResult(e.Client))
                e.Client.Dispose(); // This sample serves one peer per run.
        }
        server.ClientAccepted += Accept;
        server.Start();
        Console.WriteLine($"[tcp server] listening on {host}:{port}; waiting for one client.");
        try
        {
            using var socket = await accepted.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            await using var client = new OPNXTcpClient(socket);
            await RunProtocolClientAsync(client, role, cancellationToken);
        }
        finally
        {
            server.ClientAccepted -= Accept;
            server.Stop();
            if (accepted.Task.IsCompletedSuccessfully)
                accepted.Task.Result.Dispose();
        }
    }

    // TCP and Named Pipe clients share the SDK's OPNXClientBase directly.
    private static async Task RunProtocolClientAsync(OPNXClientBase client, string role, CancellationToken cancellationToken)
    {
        using var peer = new ChatPeer(role, (header, data, token) => client.SendDataAsync(header, data, token));
        client.PacketReceived += peer.PacketReceived;
        client.Disconnected += peer.Disconnected;
        try { await RunChatAsync(peer, role, cancellationToken); }
        finally
        {
            client.PacketReceived -= peer.PacketReceived;
            client.Disconnected -= peer.Disconnected;
        }
    }

    private static async Task RunPipeAsync(string role, string name, CancellationToken cancellationToken)
    {
        var endpoint = new NamedPipeEndPoint(name);
        if (role == "client")
        {
            await using var client = new OPNXNPipeClient();
            if (!await client.ConnectAsync(endpoint, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw new IOException("Named Pipe connection failed. Start the server first and use the same pipe name.");
            }
            await RunProtocolClientAsync(client, role, cancellationToken);
            return;
        }
        await using var server = new OPNXNPipeServer(endpoint);
        using var peer = new ChatPeer(role, (header, data, token) => server.SendDataAsync(header, data, token));
        server.PacketReceived += peer.PacketReceived;
        server.Disconnected += peer.Disconnected;
        await server.StartAsync();
        Console.WriteLine($"[pipe server] listening on {name}; start a local client with the same name.");
        try { await RunChatAsync(peer, role, cancellationToken); }
        finally
        {
            server.PacketReceived -= peer.PacketReceived;
            server.Disconnected -= peer.Disconnected;
        }
    }

    private static async Task RunSharedMemoryAsync(string role, string name, CancellationToken cancellationToken)
    {
        // Each SDK Producer/Consumer channel is one-way. Reverse the pair for the other role.
        string outgoing = name + (role == "server" ? ".server-to-client" : ".client-to-server");
        string incoming = name + (role == "server" ? ".client-to-server" : ".server-to-client");
        // Small, matching endpoints are sufficient for this text-only sample.
        await using var producer = new OPNXSMProducer(new SharedMemoryEndPoint(outgoing, 64 * 1024, 1024 * 1024));
        await using var consumer = new OPNXSMConsumer(new SharedMemoryEndPoint(incoming, 64 * 1024, 1024 * 1024));
        using var peer = new ChatPeer(role, (header, data, token) => producer.SendDataAsync(header, data, token));
        consumer.PacketReceived += peer.PacketReceived;
        Console.WriteLine($"[shm {role}] endpoint ready: {name}; start the other local process with the same name.");
        try { await RunChatAsync(peer, role, cancellationToken); }
        finally { consumer.PacketReceived -= peer.PacketReceived; }
    }

    // Transport-independent text, request/response, timeout, and cancellation handling.
    private static async Task RunChatAsync(ChatPeer peer, string role, CancellationToken cancellationToken)
    {
        await peer.WaitForPeerAsync(cancellationToken);
        Console.WriteLine($"[{role}] peer ready. Commands: /request <text>, /timeout, /cancel, exit");
        while (!cancellationToken.IsCancellationRequested)
        {
            string? line = await Console.In.ReadLineAsync(cancellationToken);
            if (line == null || line.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;
            if (!string.IsNullOrWhiteSpace(line))
                await peer.SendAsync(line, cancellationToken);
        }
    }

    private sealed record ChatRequest(Guid ID, string Message, bool Reply);
    private sealed record ChatResponse(Guid ID, string Message);

    private sealed class ChatPeer : IDisposable
    {
        private readonly Func<PacketHeader, object, CancellationToken, ValueTask<bool>> _send;
        private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly CancellationTokenSource _lifetime = new();
        private readonly string _name;
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<string>> _pending = new();

        public ChatPeer(string name, Func<PacketHeader, object, CancellationToken, ValueTask<bool>> send)
        {
            _name = name;
            _send = send;
        }

        public async Task WaitForPeerAsync(CancellationToken cancellationToken)
        {
            using var startup = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            startup.CancelAfter(TimeSpan.FromSeconds(30));
            try
            {
                while (!_ready.Task.IsCompleted)
                {
                    await _send(new PacketHeader(PacketFlags.None, PacketType.Notice, 3), "hello", startup.Token);
                    await Task.WhenAny(_ready.Task, Task.Delay(200, startup.Token));
                    startup.Token.ThrowIfCancellationRequested();
                }
                await _ready.Task;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException("The other chat process did not become ready within 30 seconds.");
            }
        }

        public void Disconnected(object? sender, DisconnectedEventArgs e)
        {
            var error = new IOException($"Disconnected: {e.Reason}");
            _ready.TrySetException(error);
            foreach (var completion in _pending.Values)
                completion.TrySetException(error);
            Console.WriteLine($"[{_name}] disconnected: {e.Reason}");
        }

        public async Task SendAsync(string message, CancellationToken cancellationToken)
        {
            bool request = message.StartsWith("/request ", StringComparison.OrdinalIgnoreCase);
            bool timeout = message.Equals("/timeout", StringComparison.OrdinalIgnoreCase);
            bool cancel = message.Equals("/cancel", StringComparison.OrdinalIgnoreCase);
            if (!request && !timeout && !cancel)
            {
                if (!await _send(TextPacketHeader, message, cancellationToken))
                    Console.WriteLine("[chat] send failed.");
                return;
            }

            var id = Guid.NewGuid();
            var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[id] = completion;
            using var requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (cancel)
                requestCancellation.CancelAfter(TimeSpan.FromMilliseconds(250));
            try
            {
                var header = new PacketHeader(PacketFlags.None, PacketType.Request, 2);
                var payload = new ChatRequest(id, request ? message[9..] : "No reply requested", request);
                if (!await _send(header, payload, requestCancellation.Token))
                {
                    Console.WriteLine("[request] send failed.");
                    return;
                }
                string response = await completion.Task.WaitAsync(TimeSpan.FromSeconds(3), requestCancellation.Token);
                Console.WriteLine($"[response] {response}");
            }
            catch (TimeoutException) { Console.WriteLine("[request] timed out after 3 seconds."); }
            catch (OperationCanceledException) { Console.WriteLine("[request] canceled."); }
            catch (IOException ex) { Console.WriteLine($"[request] {ex.Message}"); }
            finally { _pending.TryRemove(id, out _); }
        }

        public async void PacketReceived(object? sender, PacketReceivedEventArgs e)
        {
            try
            {
                if (e.Header.PayloadType == 3 && e.Header.PacketType == PacketType.Notice)
                {
                    string message = Encoding.UTF8.GetString(e.Payload.Span);
                    if (message is "hello" or "ready")
                    {
                        _ready.TrySetResult();
                        if (message == "hello")
                            await _send(new PacketHeader(PacketFlags.None, PacketType.Notice, 3), "ready", _lifetime.Token);
                    }
                }
                else if (e.Header.PayloadType == 1 && e.Header.PacketType == PacketType.Notice)
                    Console.WriteLine($"[{_name}] received: {Encoding.UTF8.GetString(e.Payload.Span)}");
                else if (e.Header.PayloadType == 2 && e.Header.PacketType == PacketType.Request)
                {
                    var request = JsonSerializer.Deserialize<ChatRequest>(e.Payload.Span);
                    if (request?.Reply == true)
                    {
                        var header = new PacketHeader(PacketFlags.None, PacketType.Response, 2);
                        if (!await _send(header, new ChatResponse(request.ID, $"Echo: {request.Message}"), _lifetime.Token))
                            Console.WriteLine("[response] send failed.");
                    }
                }
                else if (e.Header.PayloadType == 2 && e.Header.PacketType == PacketType.Response)
                {
                    var response = JsonSerializer.Deserialize<ChatResponse>(e.Payload.Span);
                    if (response != null && _pending.TryGetValue(response.ID, out var completion))
                        completion.TrySetResult(response.Message);
                }
            }
            catch (JsonException) { Console.WriteLine("[chat] invalid request/response payload."); }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (IOException ex) { Console.WriteLine($"[chat] {ex.Message}"); }
        }

        public void Dispose()
        {
            _lifetime.Cancel();
            foreach (var completion in _pending.Values)
                completion.TrySetCanceled();
            _lifetime.Dispose();
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("OPNX Protocol Chat Sample - TCP / Named Pipe / Shared Memory");
        Console.WriteLine("  dotnet run --project src/OPNX.Samples.ProtocolChat -- tcp server|client [IP] [port]");
        Console.WriteLine("  dotnet run --project src/OPNX.Samples.ProtocolChat -- pipe server|client [pipe-name]");
        Console.WriteLine("  dotnet run --project src/OPNX.Samples.ProtocolChat -- shm server|client [channel-name]");
        Console.WriteLine("Defaults: TCP 127.0.0.1:9000; IPC name opnx-protocol-chat");
        Console.WriteLine("Start one server and one client with matching transport and endpoint; startup timeout: 30 seconds.");
        Console.WriteLine("Named Pipe is local. Shared Memory is local and Windows-only in this SDK.");
        Console.WriteLine("Commands: text, /request <text>, /timeout (3 seconds), /cancel (250 ms), exit; Ctrl+C to stop.");
    }
}
