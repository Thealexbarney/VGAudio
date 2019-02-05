using System.Collections.Generic;
using VGAudio.Codecs.Opus;

namespace VGAudio.Formats.Opus
{
    public class OpusFormatBuilder : AudioFormatBaseBuilder<OpusFormat, OpusFormatBuilder, OpusParameters>
    {
        public override int ChannelCount { get; }
        public int PreSkip { get; }
        public bool HasFinalRangeSet { get; private set; }

        public List<OpusFrame> Frames { get; }

        public OpusFormatBuilder(int sampleCount, int sampleRate, int channelCount, List<OpusFrame> frames)
        : this(sampleCount, sampleRate, channelCount, 0, frames) { }

        public OpusFormatBuilder(int sampleCount, int sampleRate, int channelCount, int preSkip, List<OpusFrame> frames)
        {
            SampleCount = sampleCount;
            SampleRate = sampleRate;
            ChannelCount = channelCount;
            PreSkip = preSkip;
            Frames = frames;
        }

        public OpusFormatBuilder HasFinalRange()
        {
            HasFinalRangeSet = true;
            return this;
        }

        public override OpusFormat Build()
        {
            return new OpusFormat(this);
        }
    }
}
