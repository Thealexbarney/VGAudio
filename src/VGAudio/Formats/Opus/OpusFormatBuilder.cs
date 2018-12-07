using System.Collections.Generic;
using VGAudio.Codecs.Opus;
using VGAudio.Containers.Opus;

namespace VGAudio.Formats.Opus
{
    public class OpusFormatBuilder : AudioFormatBaseBuilder<OpusFormat, OpusFormatBuilder, OpusParameters>
    {
        public override int ChannelCount { get; }
        public int PreSkip { get; }

        public List<NxOpusFrame> Frames { get; }

        public OpusFormatBuilder(int sampleCount, int sampleRate, int channelCount, List<NxOpusFrame> frames)
        : this(sampleCount, sampleRate, channelCount, 0, frames) { }

        public OpusFormatBuilder(int sampleCount, int sampleRate, int channelCount, int preSkip, List<NxOpusFrame> frames)
        {
            SampleCount = sampleCount;
            SampleRate = sampleRate;
            ChannelCount = channelCount;
            PreSkip = preSkip;
            Frames = frames;
        }

        public override OpusFormat Build()
        {
            return new OpusFormat(this);
        }
    }
}
