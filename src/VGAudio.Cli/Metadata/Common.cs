namespace VGAudio.Cli.Metadata
{
    internal class Common
    {
        public int SampleCount { get; set; }
        public int SampleRate { get; set; }
        public int ChannelCount { get; set; }
        public bool Looping { get; set; }
        public int LoopStart { get; set; }
        public int LoopEnd { get; set; }
        public Formats Format { get; set; }
    }
}
