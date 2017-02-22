using System;
using System.Collections.Generic;

#if NET20
using DspAdpcm.Compatibility.LinqBridge;
#else
using System.Linq;
#endif

namespace DspAdpcm.Formats
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

        public override void Add(Pcm16Format pcm16)
        {
            if (pcm16.SampleCount != SampleCount)
            {
                throw new ArgumentException("Only audio streams of the same length can be added to each other.");
            }

            Channels = Channels.Concat(pcm16.Channels).ToArray();
            ChannelCount = Channels.Length;
        }

        public override Pcm16Format GetChannels(IEnumerable<int> channelRange)
        {
            if (channelRange == null)
                throw new ArgumentNullException(nameof(channelRange));

            var channels = new List<short[]>();

            foreach (int i in channelRange)
            {
                if (i < 0 || i >= Channels.Length)
                    throw new ArgumentException($"Channel {i} does not exist.", nameof(channelRange));
                channels.Add(Channels[i]);
            }

            return new Pcm16Format(SampleCount, SampleRate, channels.ToArray());
        }
    }
}
