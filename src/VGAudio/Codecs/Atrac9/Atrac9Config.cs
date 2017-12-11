using System.IO;
using VGAudio.Utilities;

namespace VGAudio.Codecs.Atrac9
{
    public class Atrac9Config
    {
        public int ChannelCount { get; }
        public int SampleRate { get; }
        public int SuperframeSize { get; }
        public byte[] ConfigData { get; }

        public int SampleRateIndex { get; }
        public int FrameSamplesPower { get; }
        public int FrameSamples { get; }
        public int ChannelConfigIndex { get; }
        public int FrameSize { get; }
        public int SuperframeIndex { get; }
        public int SuperframeSamples { get; }
        public int FramesPerSuperframe { get; }
        public bool HighSampleRate { get; }
        public ChannelConfig ChannelConfig { get; }

        public Atrac9Config(byte[] configData)
        {
            if (configData == null || configData.Length != 4)
            {
                throw new InvalidDataException("Config data must be 4 bytes long");
            }

            ReadConfigData(configData, out int a, out int b, out int c, out int d);
            SampleRateIndex = a;
            ChannelConfigIndex = b;
            FrameSize = c;
            SuperframeIndex = d;
            ConfigData = configData;

            FramesPerSuperframe = 1 << SuperframeIndex;
            SuperframeSize = FrameSize << SuperframeIndex;
            ChannelConfig = Tables.ChannelConfig[ChannelConfigIndex];

            ChannelCount = ChannelConfig.ChannelCount;
            SampleRate = Tables.SampleRates[SampleRateIndex];
            HighSampleRate = SampleRateIndex > 7;
            FrameSamplesPower = Tables.SamplingRateIndexToFrameSamplesPower[SampleRateIndex];
            FrameSamples = 1 << FrameSamplesPower;
            SuperframeSamples = FrameSamples * FramesPerSuperframe;
        }

        private static void ReadConfigData(byte[] configData, out int sampleRateIndex, out int channelConfigIndex, out int frameSize, out int superframeIndex)
        {
            var reader = new BitReader(configData);

            int header = reader.ReadInt(8);
            sampleRateIndex = reader.ReadInt(4);
            channelConfigIndex = reader.ReadInt(3);
            int validationBit = reader.ReadInt(1);
            frameSize = reader.ReadInt(11) + 1;
            superframeIndex = reader.ReadInt(2);

            if (header != 0xFE || validationBit != 0)
            {
                throw new InvalidDataException("ATRAC9 Config Data is invalid");
            }
        }
    }
}
