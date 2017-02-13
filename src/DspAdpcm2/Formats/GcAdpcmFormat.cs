using System;
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

        public List<GcAdpcmTrack> Tracks { get; set; }= new List<GcAdpcmTrack>();

        public GcAdpcmFormat(int sampleCount, int sampleRate, GcAdpcmChannel[] channels)
            : base(sampleCount, sampleRate, channels.Length)
        {
            Channels = channels;
        }

        public GcAdpcmFormat() : base(0, 0, 0)
        {
            Channels = new GcAdpcmChannel[0];
        }

        public override Pcm16Format ToPcm16()
        {
            var a = new List<short[]>();
            foreach (GcAdpcmChannel channel in Channels)
            {
                a.Add(GcAdpcmDecoder.Decode(channel.AudioData, channel.Coefs, SampleCount, channel.Hist1, channel.Hist2));
            }

            return new Pcm16Format(SampleCount, SampleRate, a.ToArray());
        }

        public override GcAdpcmFormat EncodeFromPcm16(Pcm16Format pcm16)
        {
            throw new NotImplementedException();
        }
    }
}
