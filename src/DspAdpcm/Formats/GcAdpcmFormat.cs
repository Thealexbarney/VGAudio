using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DspAdpcm.Codecs;
using DspAdpcm.Formats.GcAdpcm;
using DspAdpcm.Utilities;

#if NET20
using DspAdpcm.Compatibility.LinqBridge;
#else
using System.Linq;
#endif

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
            Parallel.For(0, Channels.Length, i =>
            {
                Channels[i].SetAlignment(multiple, base.LoopStart, base.LoopEnd);
            });
        }

        public override void SetLoop(bool loop, int loopStart, int loopEnd)
        {
            base.SetLoop(loop, loopStart, loopEnd);
            SetAlignment(AlignmentMultiple);
        }

        public override Pcm16Format ToPcm16()
        {
            var pcmChannels = new short[Channels.Length][];
            Parallel.For(0, Channels.Length, i =>
            {
                GcAdpcmChannel channel = Channels[i];
                pcmChannels[i] = GcAdpcmDecoder.Decode(channel, SampleCount);
            });

            return new Pcm16Format(SampleCount, SampleRate, pcmChannels);
        }

        public override GcAdpcmFormat EncodeFromPcm16(Pcm16Format pcm16)
        {
            var channels = new GcAdpcmChannel[pcm16.ChannelCount];

            Parallel.For(0, pcm16.ChannelCount, i =>
            {
                channels[i] = EncodeChannel(pcm16.SampleCount, pcm16.Channels[i]);
            });

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
