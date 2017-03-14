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
    public class Pcm16Format : AudioFormatBase<Pcm16Format>
    {
        public short[][] Channels { get; private set; }

        public Pcm16Format(int sampleCount, int sampleRate, short[][] channels)
            : base(sampleCount, sampleRate, channels.Length)
        {
            Channels = channels;
        }

        public Pcm16Format() : base(0, 0, 0)
        {
            Channels = new short[0][];
        }

        public override Pcm16Format ToPcm16()
        {
            return new Pcm16Format(SampleCount, SampleRate, Channels);
        }

        public override Pcm16Format EncodeFromPcm16(Pcm16Format pcm16)
        {
            return new Pcm16Format(pcm16.SampleCount, pcm16.SampleRate, pcm16.Channels);
        }

        protected override void AddInternal(Pcm16Format pcm16)
        {
            Channels = Channels.Concat(pcm16.Channels).ToArray();
            ChannelCount = Channels.Length;
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

            return new Pcm16Format(SampleCount, SampleRate, channels.ToArray());
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
