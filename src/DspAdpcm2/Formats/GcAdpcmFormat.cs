using System;
using System.Collections.Generic;
using System.Linq;
using DspAdpcm.Codecs;
using DspAdpcm.Formats.GcAdpcm;
using DspAdpcm.Utilities;

namespace DspAdpcm.Formats
{
    /// <summary>
    /// A 4-bit Nintendo ADPCM audio stream.
    /// The stream can contain any number of individual channels.
    /// </summary>
    public class GcAdpcmFormat : AudioFormatBase<GcAdpcmFormat>
    {
        public GcAdpcmChannel[] Channels { get; private set; }

        private List<GcAdpcmTrack> _tracks;
        public List<GcAdpcmTrack> Tracks
        {
            get { return _tracks == null || _tracks.Count == 0 ? GetDefaultTrackList().ToList() : _tracks; }
            set { _tracks = value; }
        }

        public int AlignmentMultiple { get; private set; }
        private int AlignmentSamples => Helpers.GetNextMultiple(base.LoopStart, AlignmentMultiple) - base.LoopStart;
        public new int LoopStart => base.LoopStart + AlignmentSamples;
        public new int LoopEnd => base.LoopEnd + AlignmentSamples;
        public new int SampleCount => AlignmentSamples == 0 ? base.SampleCount : LoopEnd;

        public GcAdpcmFormat(int sampleCount, int sampleRate, GcAdpcmChannel[] channels)
            : base(sampleCount, sampleRate, channels.Length)
        {
            Channels = channels;
        }

        public GcAdpcmFormat() : base(0, 0, 0)
        {
            Channels = new GcAdpcmChannel[0];
        }

        public void SetAlignment(int multiple)
        {
            AlignmentMultiple = multiple;
            foreach (GcAdpcmChannel channel in Channels)
            {
                channel.SetAlignment(multiple, base.LoopStart, base.LoopEnd);
            }
        }

        public override void SetLoop(int loopStart, int loopEnd)
        {
            base.SetLoop(loopStart, loopEnd);
            SetAlignment(AlignmentMultiple);
        }

        public override Pcm16Format ToPcm16()
        {
            var pcmChannels = new List<short[]>();
            foreach (GcAdpcmChannel channel in Channels)
            {
                pcmChannels.Add(GcAdpcmDecoder.Decode(channel, SampleCount));
            }

            return new Pcm16Format(SampleCount, SampleRate, pcmChannels.ToArray());
        }

        public override GcAdpcmFormat EncodeFromPcm16(Pcm16Format pcm16)
        {
            var channels = new GcAdpcmChannel[pcm16.ChannelCount];

            for (int i = 0; i < pcm16.ChannelCount; i++)
            {
                channels[i] = EncodeChannel(pcm16.SampleCount, pcm16.Channels[i]);
            }

            return new GcAdpcmFormat(pcm16.SampleCount, pcm16.SampleRate, channels);
        }

        public override void Add(GcAdpcmFormat adpcm)
        {
            if (adpcm.SampleCount != SampleCount)
            {
                throw new ArgumentException("Only audio streams of the same length can be added to each other.");
            }

            Channels = Channels.Concat(adpcm.Channels).ToArray();
            ChannelCount = Channels.Length;
            SetAlignment(AlignmentMultiple);
        }

        public override GcAdpcmFormat GetChannels(IEnumerable<int> channelRange)
        {
            if (channelRange == null)
                throw new ArgumentNullException(nameof(channelRange));

            GcAdpcmFormat copy = ShallowClone();
            var channels = new List<GcAdpcmChannel>();
            copy._tracks = null;

            foreach (int i in channelRange)
            {
                if (i < 0 || i >= Channels.Length)
                    throw new ArgumentException($"Channel {i} does not exist.", nameof(channelRange));
                channels.Add(Channels[i]);
            }
            copy.Channels = channels.ToArray();
            copy.ChannelCount = channels.Count;
            return copy;
        }

        private GcAdpcmChannel EncodeChannel(int sampleCount, short[] pcm)
        {
            short[] coefs = GcAdpcmEncoder.DspCorrelateCoefs(pcm);
            byte[] adpcm = GcAdpcmEncoder.EncodeAdpcm(pcm, coefs);

            return new GcAdpcmChannel(sampleCount, adpcm) { Coefs = coefs };
        }

        private IEnumerable<GcAdpcmTrack> GetDefaultTrackList()
        {
            int trackCount = Channels.Length.DivideByRoundUp(2);
            for (int i = 0; i < trackCount; i++)
            {
                int channelCount = Math.Min(Channels.Length - i * 2, 2);
                yield return new GcAdpcmTrack
                {
                    ChannelCount = channelCount,
                    ChannelLeft = i * 2,
                    ChannelRight = channelCount >= 2 ? i * 2 + 1 : 0
                };
            }
        }

        private GcAdpcmFormat ShallowClone() => (GcAdpcmFormat)MemberwiseClone();
    }
}
