using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VGAudio.Formats
{
    /// <summary>
    /// A 16-bit PCM audio stream.
    /// The stream can contain any number of individual channels.
    /// </summary>
    public class Pcm16Format : AudioFormatBase<Pcm16Format, Pcm16Format.Builder>
    {
        public short[][] Channels { get; }

        public Pcm16Format() => Channels = new short[0][];
        public Pcm16Format(short[][] channels, int sampleRate) : this(new Builder(channels, sampleRate)) { }
        private Pcm16Format(Builder b) : base(b) => Channels = b.Channels;

        public override Pcm16Format ToPcm16() => GetCloneBuilder().Build();
        public override Pcm16Format EncodeFromPcm16(Pcm16Format pcm16) => pcm16.GetCloneBuilder().Build();

        protected override Pcm16Format AddInternal(Pcm16Format pcm16)
        {
            Builder copy = GetCloneBuilder();
            copy.Channels = Channels.Concat(pcm16.Channels).ToArray();
            return copy.Build();
        }

        protected override Pcm16Format GetChannelsInternal(int[] channelRange)
        {
            var channels = new List<short[]>();

            foreach (int i in channelRange)
            {
                if (i < 0 || i >= Channels.Length)
                    throw new ArgumentException($"Channel {i} does not exist.", nameof(channelRange));
                channels.Add(Channels[i]);
            }

            Builder copy = GetCloneBuilder();
            copy.Channels = channels.ToArray();
            return copy.Build();
        }

        public static Builder GetBuilder(short[][] channels, int sampleRate) => new Builder(channels, sampleRate);
        public override Builder GetCloneBuilder() => GetCloneBuilderBase(new Builder(Channels, SampleRate));

        public class Builder : AudioFormatBaseBuilder<Pcm16Format, Builder>
        {
            public short[][] Channels { get; set; }
            protected internal override int ChannelCount => Channels.Length;

            public Builder(short[][] channels, int sampleRate)
            {
                if (channels == null || channels.Length < 1)
                    throw new InvalidDataException("Channels parameter cannot be empty or null");

                Channels = channels.ToArray();
                SampleCount = Channels[0]?.Length ?? 0;
                SampleRate = sampleRate;

                foreach (var channel in Channels)
                {
                    if (channel == null)
                        throw new InvalidDataException("All provided channels must be non-null");

                    if (channel.Length != SampleCount)
                        throw new InvalidDataException("All channels must have the same sample count");
                }
            }

            public override Pcm16Format Build() => new Pcm16Format(this);
        }
    }
}
