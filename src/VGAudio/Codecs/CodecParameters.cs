namespace VGAudio.Codecs
{
    public class CodecParameters
    {
        public IProgressReport Progress { get; set; }
        public int SampleCount { get; set; } = -1;

        public CodecParameters() { }
        public CodecParameters(CodecParameters source)
        {
            Progress = source.Progress;
            SampleCount = source.SampleCount;
        }
    }
}
