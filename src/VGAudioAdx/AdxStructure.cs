// ReSharper disable once CheckNamespace
namespace VGAudio.Containers.Adx
{
    public class AdxStructure
    {
        public short CopyrightOffset { get; set; }
        public AdxType EncodingType { get; set; }
        public byte FrameSize { get; set; }
        public byte BitDepth { get; set; }
        public byte ChannelCount { get; set; }
        public int SampleRate { get; set; }
        public int SampleCount { get; set; }
        public short HighpassFreq { get; set; }
        public byte Version { get; set; }
        public byte Flags { get; set; }
        public short AlignmentSamples { get; set; }
        public int LoopCount { get; set; }
        public bool Looping { get; set; }
        public int LoopStartSample { get; set; }
        public int LoopStartByte { get; set; }
        public int LoopEndSample { get; set; }
        public int LoopEndByte { get; set; }
        public short[][] HistorySamples { get; set; }
        public byte[][] AudioData { get; set; }
    }
}