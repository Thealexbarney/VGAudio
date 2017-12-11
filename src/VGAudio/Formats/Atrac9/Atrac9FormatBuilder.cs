using System;
using VGAudio.Codecs.Atrac9;

namespace VGAudio.Formats.Atrac9
{
    public class Atrac9FormatBuilder : AudioFormatBaseBuilder<Atrac9Format, Atrac9FormatBuilder, Atrac9Parameters>
    {
        public Atrac9Config Config { get; }
        public byte[][] AudioData { get; }
        public override int ChannelCount => Config.ChannelCount;
        public int EncoderDelay { get; }

        public Atrac9FormatBuilder(byte[][] audioData, Atrac9Config config, int sampleCount, int encoderDelay)
        {
            AudioData = audioData ?? throw new ArgumentNullException(nameof(audioData));
            Config = config ?? throw new ArgumentNullException(nameof(config));
            SampleRate = config.SampleRate;
            SampleCount = sampleCount;
            EncoderDelay = encoderDelay;
        }

        public override Atrac9Format Build() => new Atrac9Format(this);
    }
}
