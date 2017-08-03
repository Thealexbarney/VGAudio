using System;
using System.Collections.Generic;
using System.Linq;
using VGAudio.Codecs.GcAdpcm;
using VGAudio.Formats.Pcm16;
using VGAudio.Utilities;

namespace VGAudio.Formats.GcAdpcm
{
    /// <summary>
    /// A 4-bit Nintendo ADPCM audio stream.
    /// The stream can contain any number of individual channels.
    /// </summary>
    public class GcAdpcmFormat : AudioFormatBase<GcAdpcmFormat, GcAdpcmFormatBuilder, GcAdpcmParameters>
    {
        public GcAdpcmChannel[] Channels { get; }

        public int AlignmentMultiple { get; }
        private int AlignmentSamples => Helpers.GetNextMultiple(UnalignedLoopStart, AlignmentMultiple) - UnalignedLoopStart;
        public override int LoopStart => UnalignedLoopStart + AlignmentSamples;
        public override int LoopEnd => UnalignedLoopEnd + AlignmentSamples;
        public override int SampleCount => AlignmentSamples == 0 ? UnalignedSampleCount : LoopEnd;

        public GcAdpcmFormat() => Channels = new GcAdpcmChannel[0];
        public GcAdpcmFormat(GcAdpcmChannel[] channels, int sampleRate) : this(new GcAdpcmFormatBuilder(channels, sampleRate)) { }

        internal GcAdpcmFormat(GcAdpcmFormatBuilder b) : base(b)
        {
            Channels = b.Channels;
            AlignmentMultiple = b.AlignmentMultiple;

            Parallel.For(0, Channels.Length, i =>
            {
                Channels[i] = Channels[i]
                    .GetCloneBuilder()
                    .WithLoop(Looping, UnalignedLoopStart, UnalignedLoopEnd)
                    .WithLoopAlignment(b.AlignmentMultiple)
                    .Build();
            });
        }

        public override Pcm16Format ToPcm16()
        {
            var pcmChannels = new short[Channels.Length][];
            Parallel.For(0, Channels.Length, i =>
            {
                pcmChannels[i] = Channels[i].GetPcmAudio();
            });

            return new Pcm16FormatBuilder(pcmChannels, SampleRate)
                .WithLoop(Looping, LoopStart, LoopEnd)
                .WithTracks(Tracks)
                .Build();
        }

        public override GcAdpcmFormat EncodeFromPcm16(Pcm16Format pcm16) => EncodeFromPcm16(pcm16, null);

        public override GcAdpcmFormat EncodeFromPcm16(Pcm16Format pcm16, GcAdpcmParameters config)
        {
            var channels = new GcAdpcmChannel[pcm16.ChannelCount];

            int frameCount = pcm16.SampleCount.DivideByRoundUp(14) * pcm16.ChannelCount;
            config?.Progress?.SetTotal(frameCount);

            Parallel.For(0, pcm16.ChannelCount, i =>
            {
                channels[i] = EncodeChannel(pcm16.Channels[i], pcm16.SampleCount, config);
            });

            return new GcAdpcmFormatBuilder(channels, pcm16.SampleRate)
                .WithLoop(pcm16.Looping, pcm16.LoopStart, pcm16.LoopEnd)
                .WithTracks(pcm16.Tracks)
                .Build();
        }

        protected override GcAdpcmFormat AddInternal(GcAdpcmFormat adpcm)
        {
            GcAdpcmFormatBuilder copy = GetCloneBuilder();
            copy.Channels = Channels.Concat(adpcm.Channels).ToArray();
            return copy.Build();
        }

        protected override GcAdpcmFormat GetChannelsInternal(int[] channelRange)
        {
            var channels = new List<GcAdpcmChannel>();

            foreach (int i in channelRange)
            {
                if (i < 0 || i >= Channels.Length)
                    throw new ArgumentException($"Channel {i} does not exist.", nameof(channelRange));
                channels.Add(Channels[i]);
            }

            GcAdpcmFormatBuilder copy = GetCloneBuilder();
            copy.Channels = channels.ToArray();
            copy = copy.WithTracks(Tracks);
            return copy.Build();
        }

        public byte[] BuildSeekTable(int entryCount, Endianness endianness)
        {
            var tables = new short[Channels.Length][];

            Parallel.For(0, tables.Length, i =>
            {
                tables[i] = Channels[i].GetSeekTable();
            });

            short[] table = tables.Interleave(2);

            Array.Resize(ref table, entryCount * 2 * Channels.Length);
            return table.ToByteArray(endianness);
        }

        public static GcAdpcmFormatBuilder GetBuilder(GcAdpcmChannel[] channels, int sampleRate) => new GcAdpcmFormatBuilder(channels, sampleRate);
        public override GcAdpcmFormatBuilder GetCloneBuilder()
        {
            var builder = new GcAdpcmFormatBuilder(Channels, SampleRate);
            builder = GetCloneBuilderBase(builder);
            builder = builder.WithTracks(Tracks);
            builder.AlignmentMultiple = AlignmentMultiple;
            return builder;
        }

        public GcAdpcmFormat WithAlignment(int loopStartAlignment) => GetCloneBuilder()
            .WithAlignment(loopStartAlignment)
            .Build();

        private static GcAdpcmChannel EncodeChannel(short[] pcm, int sampleCount, GcAdpcmParameters config)
        {
            short[] coefs = GcAdpcmCoefficients.CalculateCoefficients(pcm);
            byte[] adpcm = GcAdpcmEncoder.Encode(pcm, coefs, config);

            return new GcAdpcmChannel(adpcm, coefs, sampleCount);
        }
    }
}
