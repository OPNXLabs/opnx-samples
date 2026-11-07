using FFmpeg.AutoGen;

namespace OPNX.Samples.RtspViewer.Services;

public sealed unsafe class DecodedVideoFrame : IDisposable
{
    private AVFrame* _frame;

    private DecodedVideoFrame(AVFrame* frame, double fps)
    {
        _frame = frame;
        Width = frame->width;
        Height = frame->height;
        Fps = fps;
    }

    public AVFrame* Frame => _frame;

    public int Width { get; }

    public int Height { get; }

    public double Fps { get; }

    public static DecodedVideoFrame CreateRef(AVFrame* sourceFrame, double fps)
    {
        if (sourceFrame == null)
            throw new ArgumentNullException(nameof(sourceFrame));

        AVFrame* frame = ffmpeg.av_frame_alloc();
        if (frame == null)
            throw new InvalidOperationException("ffmpeg frame allocation failed");

        int result = ffmpeg.av_frame_ref(frame, sourceFrame);
        if (result < 0)
        {
            ffmpeg.av_frame_free(&frame);
            throw new InvalidOperationException($"frame reference failed: {result}");
        }

        return new DecodedVideoFrame(frame, fps);
    }

    public void Dispose()
    {
        if (_frame == null)
            return;

        AVFrame* frame = _frame;
        _frame = null;
        ffmpeg.av_frame_free(&frame);
    }
}
