using System;
using System.Collections.Generic;
using System.Linq;
using VGAudio.Utilities;

namespace VGAudio.Formats
{
    /// <summary>
    /// A 16-bit PCM audio stream.
    /// The stream can contain any number of individual channels.
    /// </summary>
    public class Pcm16Format : AudioFormatBase<Pcm16Format, Pcm16Format.Builder>
    {
        public short[][] Channels { get; }

        public Pcm16Format() : base(0, 0, 0) => Channels = new short[0][];
        public Pcm16Format(int sampleCount, int sampleRate, short[][] channels)
            : base(sampleCount, sampleRate, channels.Length)
        {
            Channels = channels;
        }

        private Pcm16Format(Builder b)
            : base(b.SampleCount, b.SampleRate, b.Channels.Length)
        {
            Channels = b.Channels;
        }

        public override Pcm16Format ToPcm16() => GetCloneBuilder().Build();
        public override Pcm16Format EncodeFromPcm16(Pcm16Format pcm16) => pcm16.GetCloneBuilder().Build();

        protected override Pcm16Format AddInternal(Pcm16Format pcm16)
        {
            Builder copy = GetCloneBuilder();
            copy.Channels = Channels.Concat(pcm16.Channels).ToArray();
            return copy.Build();
        }

        protected override Pcm16Format GetChannelsInternal(IEnumerable<int> channelRange)
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

        public static Builder GetBuilder() => new Builder();
        public override Builder GetCloneBuilder()
        {
            Builder builder = base.GetCloneBuilder();
            builder.Channels = Channels;
            return builder;
        }

        public class Builder : AudioFormatBaseBuilder<Pcm16Format, Builder>
        {
            public short[][] Channels { get; set; }
            internal override int ChannelCount => Channels.Length;

            public override Pcm16Format Build() => new Pcm16Format(this);
        }

        public override bool Equals(object obj)
        {
            var item = obj as Pcm16Format;

            if (item == null)
            {
                return false;
            }

            return
                item.SampleCount == SampleCount &&
                item.SampleRate == SampleRate &&
                item.LoopStart == LoopStart &&
                item.LoopEnd == LoopEnd &&
                item.Looping == Looping &&
                !Channels.Where((t, i) => !Helpers.ArraysEqual(item.Channels[i], t)).Any();
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = SampleCount.GetHashCode();
                hashCode = (hashCode * 397) ^ SampleRate.GetHashCode();
                hashCode = (hashCode * 397) ^ LoopStart.GetHashCode();
                hashCode = (hashCode * 397) ^ LoopEnd.GetHashCode();
                hashCode = (hashCode * 397) ^ Looping.GetHashCode();
                return hashCode;
            }
        }
    }
}
