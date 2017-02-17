using System.Collections.Generic;
using DspAdpcm.Codecs;
using DspAdpcm.Formats.GcAdpcm;

namespace DspAdpcm.Formats
{
    /// <summary>
    /// A 4-bit Nintendo ADPCM audio stream.
    /// The stream can contain any number of individual channels.
    /// </summary>
    public class GcAdpcmFormat : AudioFormatBase<GcAdpcmFormat>
    {
        public GcAdpcmChannel[] Channels { get; }

        public List<GcAdpcmTrack> Tracks { get; set; } = new List<GcAdpcmTrack>();

        public bool AlignmentNeeded { get; private set; }
        public int AlignmentMultiple { get; private set; }

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

        private GcAdpcmChannel EncodeChannel(int sampleCount, short[] pcm)
        {
            short[] coefs = GcAdpcmEncoder.DspCorrelateCoefs(pcm);
            byte[] adpcm = GcAdpcmEncoder.EncodeAdpcm(pcm, coefs);

            return new GcAdpcmChannel(sampleCount, adpcm) { Coefs = coefs };
        }
    }
}
