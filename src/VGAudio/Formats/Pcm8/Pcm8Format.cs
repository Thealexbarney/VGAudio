using System;
using System.Collections.Generic;
using System.Linq;
using VGAudio.Codecs;
using VGAudio.Codecs.Pcm8;
using VGAudio.Formats.Pcm16;

namespace VGAudio.Formats.Pcm8
{
    public class Pcm8Format : AudioFormatBase<Pcm8Format, Pcm8FormatBuilder, CodecParameters>
    {
        public byte[][] Channels { get; }
        public virtual bool Signed { get; } = false;

        public Pcm8Format() => Channels = new byte[0][];
        public Pcm8Format(byte[][] channels, int sampleRate) : this(new Pcm8FormatBuilder(channels, sampleRate)) { }
        internal Pcm8Format(Pcm8FormatBuilder b) : base(b) => Channels = b.Channels;

        public override Pcm16Format ToPcm16()
        {
            var channels = new short[ChannelCount][];

            for (int i = 0; i < ChannelCount; i++)
            {
                channels[i] = Signed ? Pcm8Codec.DecodeSigned(Channels[i]) : Pcm8Codec.Decode(Channels[i]);
            }

            return new Pcm16FormatBuilder(channels, SampleRate)
                .WithLoop(Looping, LoopStart, LoopEnd)
                .WithTracks(Tracks)
                .Build();
        }

        public override Pcm8Format EncodeFromPcm16(Pcm16Format pcm16)
        {
            var channels = new byte[pcm16.ChannelCount][];

            for (int i = 0; i < pcm16.ChannelCount; i++)
            {
                channels[i] = Signed ? Pcm8Codec.EncodeSigned(pcm16.Channels[i]) : Pcm8Codec.Encode(pcm16.Channels[i]);
            }

            return new Pcm8FormatBuilder(channels, pcm16.SampleRate, Signed)
                .WithLoop(pcm16.Looping, pcm16.LoopStart, pcm16.LoopEnd)
                .WithTracks(pcm16.Tracks)
                .Build();
        }

        protected override Pcm8Format AddInternal(Pcm8Format pcm8)
        {
            Pcm8FormatBuilder copy = GetCloneBuilder();
            copy.Channels = Channels.Concat(pcm8.Channels).ToArray();
            return copy.Build();
        }

        protected override Pcm8Format GetChannelsInternal(int[] channelRange)
        {
            var channels = new List<byte[]>();

            foreach (int i in channelRange)
            {
                if (i < 0 || i >= Channels.Length)
                    throw new ArgumentException($"Channel {i} does not exist.", nameof(channelRange));
                channels.Add(Channels[i]);
            }

            Pcm8FormatBuilder copy = GetCloneBuilder();
            copy.Channels = channels.ToArray();
            return copy.Build();
        }

        public static Pcm8FormatBuilder GetBuilder(byte[][] channels, int sampleRate) => new Pcm8FormatBuilder(channels, sampleRate);
        public override Pcm8FormatBuilder GetCloneBuilder() => GetCloneBuilderBase(new Pcm8FormatBuilder(Channels, SampleRate));
    }
}
