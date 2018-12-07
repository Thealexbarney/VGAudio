using System.Collections.Generic;
using VGAudio.Containers.Opus;
using VGAudio.Formats;

namespace VGAudio.Codecs.Opus
{
    public class OpusFormatBuilder : AudioFormatBaseBuilder<OpusFormat, OpusFormatBuilder, OpusParameters>
    {
        public override int ChannelCount { get; }
        public int EncoderDelay { get; }

        public List<NxOpusFrame> Frames { get; }

        public OpusFormatBuilder(int channelCount, int sampleCount, List<NxOpusFrame> frames)
        : this(channelCount, sampleCount, 0, frames) { }

        public OpusFormatBuilder(int channelCount, int sampleCount, int encoderDelay, List<NxOpusFrame> frames)
        {
            ChannelCount = channelCount;
            SampleCount = sampleCount;
            EncoderDelay = encoderDelay;
            SampleRate = 48000;
            Frames = frames;
        }

        public override OpusFormat Build()
        {
            return new OpusFormat(this);
        }
    }
}
