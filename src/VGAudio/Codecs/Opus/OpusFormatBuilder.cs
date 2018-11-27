using System.Collections.Generic;
using VGAudio.Containers.Opus;
using VGAudio.Formats;

namespace VGAudio.Codecs.Opus
{
    public class OpusFormatBuilder : AudioFormatBaseBuilder<OpusFormat, OpusFormatBuilder, OpusParameters>
    {
        public override int ChannelCount { get; }
        public List<NxOpusFrame> Frames { get; }

        public OpusFormatBuilder(int channelCount, int sampleCount, List<NxOpusFrame> frames)
        {
            ChannelCount = channelCount;
            SampleCount = sampleCount;
            SampleRate = 48000;
            Frames = frames;
        }

        public override OpusFormat Build()
        {
            return new OpusFormat(this);
        }
    }
}
