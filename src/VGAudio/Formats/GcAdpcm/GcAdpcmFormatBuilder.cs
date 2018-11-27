using System.Collections.Generic;
using System.IO;
using System.Linq;
using VGAudio.Codecs.GcAdpcm;

namespace VGAudio.Formats.GcAdpcm
{
    public class GcAdpcmFormatBuilder : AudioFormatBaseBuilder<GcAdpcmFormat, GcAdpcmFormatBuilder, GcAdpcmParameters>
    {
        public GcAdpcmChannel[] Channels { get; set; }
        public int AlignmentMultiple { get; set; }
        public override int ChannelCount => Channels.Length;

        public GcAdpcmFormatBuilder(IReadOnlyCollection<GcAdpcmChannel> channels, int sampleRate)
        {
            if (channels == null || channels.Count < 1)
                throw new InvalidDataException("Channels parameter cannot be empty or null");

            Channels = channels.ToArray();
            SampleCount = Channels[0]?.UnalignedSampleCount ?? 0;
            SampleRate = sampleRate;

            foreach (GcAdpcmChannel channel in Channels)
            {
                if (channel == null)
                    throw new InvalidDataException("All provided channels must be non-null");

                if (channel.UnalignedSampleCount != SampleCount)
                    throw new InvalidDataException("All channels must have the same sample count");
            }
        }

        public override GcAdpcmFormat Build() => new GcAdpcmFormat(this);

        public GcAdpcmFormatBuilder WithAlignment(int loopAlignmentMultiple)
        {
            AlignmentMultiple = loopAlignmentMultiple;
            return this;
        }
    }
}