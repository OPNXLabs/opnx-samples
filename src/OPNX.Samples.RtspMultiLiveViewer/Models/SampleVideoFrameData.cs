namespace OPNX.Samples.RtspMultiLiveViewer.Models
{
    public class SampleVideoFrameData(string codec, double fps, double bitrate, long timeStamp, bool isKeyFrame, IEnumerable<ReadOnlyMemory<byte>> nals)
    {
        public string Codec { get; } = codec;
        public double FPS { get; } = fps;

        public double BitRate { get; } = bitrate;

        public long TimeStamp { get; } = timeStamp;

        public bool IsKeyFrame { get; } = isKeyFrame;

        public IEnumerable<ReadOnlyMemory<byte>> Nals { get; private set; } = nals;
    }
}
