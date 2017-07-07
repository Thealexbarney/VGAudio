using VGAudio.Codecs.CriAdx;
using VGAudio.Formats.CriAdx;
using VGAudio.Utilities;

namespace VGAudio.Containers.Adx
{
    public class AdxStructure
    {
        public short HeaderSize { get; set; }
        public CriAdxType EncodingType { get; set; }
        public CriAdxKey EncryptionKey { get; set; }
        public byte FrameSize { get; set; }
        public int SamplesPerFrame => CriAdxHelpers.NibbleCountToSampleCount(FrameSize * 2, FrameSize);
        public byte BitDepth { get; set; }
        public byte ChannelCount { get; set; }
        public int SampleRate { get; set; }
        public int SampleCount { get; set; }
        public short HighpassFreq { get; set; }
        public byte Version { get; set; }
        public byte Revision { get; set; }
        public short InsertedSamples { get; set; }
        public int LoopCount { get; set; }
        public bool Looping { get; set; }
        public int LoopType { get; set; }
        public int LoopStartSample { get; set; }
        public int LoopStartByte { get; set; }
        public int LoopEndSample { get; set; }
        public int LoopEndByte { get; set; }
        public short[][] HistorySamples { get; set; }
        public int AudioDataLength => SampleCount.DivideByRoundUp(SamplesPerFrame) * FrameSize * ChannelCount;
        public byte[][] AudioData { get; set; }
    }
}