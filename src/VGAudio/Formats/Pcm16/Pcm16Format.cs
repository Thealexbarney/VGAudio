using System;
using System.Collections.Generic;
using System.Linq;
using VGAudio.Codecs;

namespace VGAudio.Formats.Pcm16
{
    /// <summary>
    /// A 16-bit PCM audio stream.
    /// The stream can contain any number of individual channels.
    /// </summary>
    public class Pcm16Format : AudioFormatBase<Pcm16Format, Pcm16FormatBuilder, CodecParameters>
    {
        public short[][] Channels { get; }

        public Pcm16Format() => Channels = new short[0][];
        public Pcm16Format(short[][] channels, int sampleRate) : this(new Pcm16FormatBuilder(channels, sampleRate)) { }
        internal Pcm16Format(Pcm16FormatBuilder b) : base(b) => Channels = b.Channels;

        public override Pcm16Format ToPcm16() => GetCloneBuilder().Build();
        public override Pcm16Format EncodeFromPcm16(Pcm16Format pcm16) => pcm16.GetCloneBuilder().Build();

        protected override Pcm16Format AddInternal(Pcm16Format pcm16)
        {
            Pcm16FormatBuilder copy = GetCloneBuilder();
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

            Pcm16FormatBuilder copy = GetCloneBuilder();
            copy.Channels = channels.ToArray();
            return copy.Build();
        }

        public static Pcm16FormatBuilder GetBuilder(short[][] channels, int sampleRate) => new Pcm16FormatBuilder(channels, sampleRate);
        public override Pcm16FormatBuilder GetCloneBuilder() => GetCloneBuilderBase(new Pcm16FormatBuilder(Channels, SampleRate));

        
    }
}
