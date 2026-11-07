using FFmpeg.AutoGen;
using OPNX.Lib.Streaming.RTSP;
using OPNX.Lib.Streaming.RTSP.Commons;
using OPNX.Lib.Streaming.RTSP.Commons.Interfaces;
using OPNX.Samples.RtspViewer.Models;
using OPNX.Samples.RtspViewer.Worker;

namespace OPNX.Samples.RtspViewer.Services;

public sealed class RtspStreamSession : IDisposable
{
    private readonly SampleCamera _camera;
    private readonly Action<DecodedVideoFrame> _onFrame;
    private readonly Action<string> _onStatus;
    private readonly RTSPClient _rtspClient;
    private readonly object _decoderLock = new();
    private SimpleDecodingWorker? _videoDecoder;
    private bool _disposed;

    public RtspStreamSession(SampleCamera camera, Action<DecodedVideoFrame> onFrame, Action<string> onStatus)
    {
        _camera = camera;
        _onFrame = onFrame;
        _onStatus = onStatus;

        _rtspClient = new RTSPClient(CreateVideoSource(camera));
        _rtspClient.Started += RtspClient_Started;
        _rtspClient.Stopped += RtspClient_Stopped;
        _rtspClient.NalUnitExtracted += RtspClient_NalUnitExtracted;
        _rtspClient.StreamConfigured += RtspClient_StreamConfigured;
    }

    public void Start()
    {
        if (_disposed)
            return;

        _onStatus("Connecting");
        _rtspClient.Start();
    }

    public async Task StopAsync()
    {
        if (_disposed)
            return;

        await _rtspClient.StopAsync(RTSPClientStopReason.COMMAND).ConfigureAwait(false);
        _onStatus("Stopped");
    }

    private static VideoSource CreateVideoSource(SampleCamera camera)
    {
        return new VideoSource
        {
            EntityID = camera.Id,
            RtspURL = camera.RtspUrl,
            RtspID = camera.Account,
            RtspPW = camera.Password,
            UseTCP = true
        };
    }

    private void RtspClient_Started(object? sender, StreamStartedEventArgs e)
    {
        _onStatus("Streaming");
    }

    private void RtspClient_Stopped(object? sender, StreamStoppedEventArgs e)
    {
        if (_disposed)
            return;

        _onStatus(e.StopReason == RTSPClientStopReason.COMMAND ? "Stopped" : $"Error: {e.StopReason}");
    }

    private void RtspClient_StreamConfigured(object? sender, StreamConfigurationDataEventArgs e)
    {
        if (e.ChannelType == ChannelTypes.Video)
            EnsureVideoDecoder(e.PayloadName);
    }

    private void RtspClient_NalUnitExtracted(object? sender, NalUnitDataEventArgs e)
    {
        if (_disposed || e.ChannelType != ChannelTypes.Video)
            return;

        var decoder = EnsureVideoDecoder(e.PayloadName);
        decoder.AddVideoFrameData(new SampleVideoFrameData(
            e.PayloadName,
            _rtspClient.FPS,
            _rtspClient.BitRate,
            e.TimeStamp,
            e.IsKeyFrame,
            e.Data));
    }

    private unsafe SimpleDecodingWorker EnsureVideoDecoder(string codec)
    {
        lock (_decoderLock)
        {
            if (_videoDecoder != null)
                return _videoDecoder;

            _videoDecoder = new SimpleDecodingWorker(codec);
            _videoDecoder.VideoFrameDataDecoded += VideoDecoder_VideoFrameDataDecoded;
            return _videoDecoder;
        }
    }

    private unsafe void VideoDecoder_VideoFrameDataDecoded(AVCodecID codecID, AVFrame* decodedFrame)
    {
        if (_disposed)
            return;

        var frame = DecodedVideoFrame.CreateRef(decodedFrame, _rtspClient.FPS);
        try
        {
            _onFrame(frame);
        }
        catch
        {
            frame.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _rtspClient.Started -= RtspClient_Started;
        _rtspClient.Stopped -= RtspClient_Stopped;
        _rtspClient.NalUnitExtracted -= RtspClient_NalUnitExtracted;
        _rtspClient.StreamConfigured -= RtspClient_StreamConfigured;

        _rtspClient.Stop(RTSPClientStopReason.COMMAND);

        lock (_decoderLock)
        {
            if (_videoDecoder != null)
            {
                unsafe
                {
                    _videoDecoder.VideoFrameDataDecoded -= VideoDecoder_VideoFrameDataDecoded;
                }
                _videoDecoder.Dispose();
                _videoDecoder = null;
            }
        }
    }
}
