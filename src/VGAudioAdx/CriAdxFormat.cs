using System;
using System.IO;
using VGAudio.Codecs;
using VGAudio.Utilities;

// ReSharper disable once CheckNamespace
namespace VGAudio.Formats
{
    public class CriAdxFormat : AudioFormatBase<CriAdxFormat, CriAdxFormat.Builder>
    {
        public byte[][] Channels { get; }
        public short HighpassFrequency { get; }
        public int FrameSize { get; }

        public CriAdxFormat() => Channels = new byte[0][];
        private CriAdxFormat(Builder b) : base(b)
        {
            Channels = b.Channels;
            FrameSize = b.FrameSize;
            HighpassFrequency = b.HighpassFrequency;
        }

        public override Pcm16Format ToPcm16()
        {
            var pcmChannels = new short[Channels.Length][];
            Parallel.For(0, Channels.Length, i =>
            {
                pcmChannels[i] = CriAdxCodec.Decode(Channels[i], SampleCount, SampleRate, FrameSize, HighpassFrequency);
            });

            return new Pcm16Format.Builder(pcmChannels, SampleRate)
                .WithLoop(Looping, LoopStart, LoopEnd)
                .WithTracks(Tracks)
                .Build();
        }

        public override CriAdxFormat EncodeFromPcm16(Pcm16Format pcm16)
        {
            throw new NotImplementedException();
        }

        protected override CriAdxFormat GetChannelsInternal(int[] channelRange)
        {
            throw new NotImplementedException();
        }

        protected override CriAdxFormat AddInternal(CriAdxFormat format)
        {
            throw new NotImplementedException();
        }

        public override Builder GetCloneBuilder()
        {
            throw new NotImplementedException();
        }

        public class Builder : AudioFormatBaseBuilder<CriAdxFormat, Builder>
        {
            public byte[][] Channels { get; set; }
            public short HighpassFrequency { get; set; }
            public int FrameSize { get; set; }
            protected override int ChannelCount => Channels.Length;

            public Builder(byte[][] channels, int sampleCount, int sampleRate, int frameSize, short highpassFrequency)
            {
                if (channels == null || channels.Length < 1)
                    throw new InvalidDataException("Channels parameter cannot be empty or null");

                Channels = channels;
                SampleCount = sampleCount;
                SampleRate = sampleRate;
                FrameSize = frameSize;
                HighpassFrequency = highpassFrequency;

                int length = Channels[0]?.Length ?? 0;
                foreach (byte[] channel in Channels)
                {
                    if (channel == null)
                        throw new InvalidDataException("All provided channels must be non-null");

                    if (channel.Length != length)
                        throw new InvalidDataException("All channels must have the same length");
                }
            }

            public override CriAdxFormat Build() => new CriAdxFormat(this);
        }
    }
}
