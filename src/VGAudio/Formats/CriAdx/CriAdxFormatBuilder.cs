using System.IO;
using VGAudio.Codecs.CriAdx;

namespace VGAudio.Formats.CriAdx
{
    public class CriAdxFormatBuilder : AudioFormatBaseBuilder<CriAdxFormat, CriAdxFormatBuilder, CriAdxParameters>
    {
        public CriAdxChannel[] Channels { get; set; }
        public short HighpassFrequency { get; set; }
        public int FrameSize { get; set; }
        public int AlignmentSamples { get; set; }
        public CriAdxType Type { get; set; } = CriAdxType.Linear;
        public int Version => Channels?[0]?.Version ?? 0;
        public override int ChannelCount => Channels.Length;

        public CriAdxFormatBuilder(CriAdxChannel[] channels, int sampleCount, int sampleRate, int frameSize, short highpassFrequency)
        {
            if (channels == null || channels.Length < 1)
                throw new InvalidDataException("Channels parameter cannot be empty or null");

            Channels = channels;
            SampleCount = sampleCount;
            SampleRate = sampleRate;
            FrameSize = frameSize;
            HighpassFrequency = highpassFrequency;

            int length = Channels[0]?.Audio?.Length ?? 0;
            foreach (CriAdxChannel channel in Channels)
            {
                if (channel == null)
                    throw new InvalidDataException("All provided channels must be non-null");

                if (channel.Audio?.Length != length)
                    throw new InvalidDataException("All channels must have the same length");
            }
        }

        public CriAdxFormatBuilder WithEncodingType(CriAdxType type)
        {
            Type = type;
            return this;
        }

        public CriAdxFormatBuilder WithAlignmentSamples(int alignmentSamplesCount)
        {
            AlignmentSamples = alignmentSamplesCount;
            return this;
        }

        public override CriAdxFormat Build() => new CriAdxFormat(this);
    }
}
