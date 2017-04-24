using System;
using System.Collections.Generic;
using System.Linq;
using VGAudio.Codecs;
using VGAudio.Formats.GcAdpcm;
using VGAudio.Utilities;

namespace VGAudio.Formats
{
    /// <summary>
    /// A 4-bit Nintendo ADPCM audio stream.
    /// The stream can contain any number of individual channels.
    /// </summary>
    public class GcAdpcmFormat : AudioFormatBase<GcAdpcmFormat, GcAdpcmFormat.Builder>
    {
        public GcAdpcmChannel[] Channels { get; }
        public List<GcAdpcmTrack> Tracks { get; }

        public int AlignmentMultiple { get; }
        private int AlignmentSamples => Helpers.GetNextMultiple(base.LoopStart, AlignmentMultiple) - base.LoopStart;
        public new int LoopStart => base.LoopStart + AlignmentSamples;
        public new int LoopEnd => base.LoopEnd + AlignmentSamples;
        public new int SampleCount => AlignmentSamples == 0 ? base.SampleCount : LoopEnd;

        public int UnalignedLoopStart => base.LoopStart;
        public int UnalignedLoopEnd => base.LoopEnd;
        public int UnalignedSampleCount =>  base.SampleCount;

        public GcAdpcmFormat() : base(0, 0, 0) => Channels = new GcAdpcmChannel[0];
        public GcAdpcmFormat(int sampleCount, int sampleRate, GcAdpcmChannel[] channels)
            : base(sampleCount, sampleRate, channels.Length)
        {
            Channels = channels;
        }

        private GcAdpcmFormat(Builder b) : base(b)
        {
            Channels = b.Channels;
            Tracks = b.Tracks == null || b.Tracks.Count == 0 ? GetDefaultTrackList(b.Channels.Length).ToList() : b.Tracks;
            AlignmentMultiple = b.AlignmentMultiple;

            Parallel.For(0, Channels.Length, i =>
            {
                var builder = Channels[i].GetCloneBuilder();
                builder.LoopAlignmentMultiple = b.AlignmentMultiple;
                Channels[i] = builder.Build();
            });
        }
        
        public override Pcm16Format ToPcm16()
        {
            var pcmChannels = new short[Channels.Length][];
            Parallel.For(0, Channels.Length, i =>
            {
                pcmChannels[i] = Channels[i].GetPcmAudio();
            });

            return new Pcm16Format(SampleCount, SampleRate, pcmChannels)
                .WithLoop(Looping, LoopStart, LoopEnd);
        }

        public override GcAdpcmFormat EncodeFromPcm16(Pcm16Format pcm16)
        {
            var channels = new GcAdpcmChannel[pcm16.ChannelCount];

            Parallel.For(0, pcm16.ChannelCount, i =>
            {
                channels[i] = EncodeChannel(pcm16.SampleCount, pcm16.Channels[i]);
            });

            return new GcAdpcmFormat(pcm16.SampleCount, pcm16.SampleRate, channels)
                .WithLoop(pcm16.Looping, pcm16.LoopStart, pcm16.LoopEnd);
        }

        protected override GcAdpcmFormat AddInternal(GcAdpcmFormat adpcm)
        {
            Builder copy = GetCloneBuilder();
            copy.Channels = Channels.Concat(adpcm.Channels).ToArray();
            return copy.Build();
        }

        protected override GcAdpcmFormat GetChannelsInternal(IEnumerable<int> channelRange)
        {
            var channels = new List<GcAdpcmChannel>();

            foreach (int i in channelRange)
            {
                if (i < 0 || i >= Channels.Length)
                    throw new ArgumentException($"Channel {i} does not exist.", nameof(channelRange));
                channels.Add(Channels[i]);
            }

            Builder copy = GetCloneBuilder();
            copy.Channels = channels.ToArray();
            copy.Tracks = null;
            return copy.Build();
        }

        public static Builder GetBuilder() => new Builder();
        public override Builder GetCloneBuilder()
        {
            Builder builder = base.GetCloneBuilder();
            builder.Tracks = Tracks;
            builder.Channels = Channels;
            builder.AlignmentMultiple = AlignmentMultiple;
            return builder;
        }

        public class Builder : AudioFormatBaseBuilder<GcAdpcmFormat, Builder>
        {
            public GcAdpcmChannel[] Channels { get; set; }
            public List<GcAdpcmTrack> Tracks { get; set; }
            public int AlignmentMultiple { get; set; }
            internal override int ChannelCount => Channels.Length;

            public override GcAdpcmFormat Build() => new GcAdpcmFormat(this);
        }

        private static GcAdpcmChannel EncodeChannel(int sampleCount, short[] pcm)
        {
            short[] coefs = GcAdpcmEncoder.DspCorrelateCoefs(pcm);
            byte[] adpcm = GcAdpcmEncoder.EncodeAdpcm(pcm, coefs);

            return new GcAdpcmChannel(adpcm, coefs, sampleCount);
        }

        private static IEnumerable<GcAdpcmTrack> GetDefaultTrackList(int channelCount)
        {
            int trackCount = channelCount.DivideByRoundUp(2);
            for (int i = 0; i < trackCount; i++)
            {
                int trackChannelCount = Math.Min(channelCount - i * 2, 2);
                yield return new GcAdpcmTrack
                {
                    ChannelCount = trackChannelCount,
                    ChannelLeft = i * 2,
                    ChannelRight = trackChannelCount >= 2 ? i * 2 + 1 : 0
                };
            }
        }

        public override bool Equals(object obj)
        {
            var item = obj as GcAdpcmFormat;

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
                Helpers.ArraysEqual(item.Tracks.ToArray(), Tracks.ToArray()) &&
                Helpers.ArraysEqual(item.Channels.ToArray(), Channels.ToArray());
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
