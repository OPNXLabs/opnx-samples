using FFmpeg.AutoGen;
using OPNX.Lib.Media.FFMpeg;
using OPNX.Lib.Media.FFMpeg.EventHandlers;
using OPNX.Lib.Media.FFMpeg.RawFrames.Video;
using OPNX.Samples.RtspMultiLiveViewer.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;

namespace OPNX.Samples.RtspMultiLiveViewer.Worker
{
    public class SimpleDecodingWorker : IDisposable
    {
        private readonly FFmpegVideoDecoder _decoder;

        private bool _disposed = false;

        private const int MaxVideoQueueSize = 5;
        private const int MinDelay = 5;
        private const int NormalDelay = 10;

        protected readonly ConcurrentQueue<SampleVideoFrameData> _videoFrameDatas = new();

        private readonly CancellationTokenSource _decodeFrameCancelToken = new();
        private readonly Thread _decodeThread;
        private readonly RawVideoFrameFactory _rawVideoFrameFactory = new();

        public SimpleDecodingWorker(string codec)
        {
            _decoder = new FFmpegVideoDecoder(FFmpegHelper.GetCodecIDFromName(codec));
            _decoder.VideoFrameDecoded += Decoder_VideoFrameDecoded;

            _decodeThread = new Thread(DecodeFrameLoop)
            {
                IsBackground = true,                
                Priority = ThreadPriority.AboveNormal
            };
            _decodeThread.Start(_decodeFrameCancelToken.Token);
        }

        public unsafe delegate void VideoFrameDataDecodedEventHandler(AVCodecID codecID, AVFrame* decodedFrame);
        public event VideoFrameDataDecodedEventHandler? VideoFrameDataDecoded;

        private void Decoder_VideoFrameDecoded(object sender, VideoFrameDecodedEventArgs e)
        {
            if (_disposed)
                return;

            unsafe
            {
                VideoFrameDataDecoded?.Invoke(e.CodecID, e.VideoFrame);
            }
        }

        private void DecodeFrameLoop(object? obj)
        {
            var cancellationToken = obj is CancellationToken token ? token : CancellationToken.None;
            bool isFirstFrameDecoded = false;
            double accumulatedDelay = 0;

            try
            {
                while (!_disposed && !cancellationToken.IsCancellationRequested)
                {
                    double delay = NormalDelay;

                    if (!_videoFrameDatas.TryDequeue(out var videoFrameData))
                    {
                        if (isFirstFrameDecoded)
                            accumulatedDelay += NormalDelay;

                        Thread.Sleep(isFirstFrameDecoded ? MinDelay : 1);
                        continue;
                    }
                    

                    var rawVideoFrame = _rawVideoFrameFactory.CreateVideoFrame(
                        videoFrameData.Codec,
                        videoFrameData.TimeStamp,
                        videoFrameData.FPS,
                        videoFrameData.Nals);

                    if (rawVideoFrame == null)
                        continue;

                    isFirstFrameDecoded = true;
                    bool decodeSuccessful = false;
                    var decodeStart = DateTime.UtcNow;

                    try
                    {
                        unsafe { decodeSuccessful = _decoder.TryDecode(rawVideoFrame, Size.Empty, AVPixelFormat.AV_PIX_FMT_BGR24); }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex, "Error decoding live video frame.");
                    }

                    if (!decodeSuccessful)
                    {
                        accumulatedDelay += NormalDelay;
                        continue;
                    }

                    var decodeEnd = DateTime.UtcNow;
                    double decodeTimeMs = (decodeEnd - decodeStart).TotalMilliseconds;
                    double frameIntervalMs = 1000.0 / rawVideoFrame.FPS;
                    int queueSize = _videoFrameDatas.Count;
                    double delayAdjustment = queueSize > MaxVideoQueueSize ? (queueSize - MaxVideoQueueSize) * 0.3 : 0;

                    delay = Math.Max(frameIntervalMs - decodeTimeMs - delayAdjustment - accumulatedDelay, MinDelay);
                    if (delay > frameIntervalMs)
                        delay = frameIntervalMs;

                    accumulatedDelay = 0;
                    Thread.Sleep((int)delay);
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public void AddVideoFrameData(SampleVideoFrameData videoFrameData)
        {
            if (_disposed)
                return;

            _videoFrameDatas.Enqueue(videoFrameData);
        }

        public void Dispose()
        {
            if(_disposed) return;

            _disposed = true;
            try
            {
                if (!_decodeFrameCancelToken.IsCancellationRequested)
                    _decodeFrameCancelToken.Cancel();

                if (!_decodeThread.Join(TimeSpan.FromSeconds(2)))
                    Debug.WriteLine("[Live] Decode thread did not stop gracefully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "Error stopping live decode thread.");
            }

            _rawVideoFrameFactory.Dispose();
            _decodeFrameCancelToken.Dispose();
            _decoder.VideoFrameDecoded -= Decoder_VideoFrameDecoded;            
        }
    }
}
