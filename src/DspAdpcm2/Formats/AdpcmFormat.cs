using System.Collections.Generic;
using DspAdpcm.Codecs;
using DspAdpcm.Formats.Adpcm;

namespace DspAdpcm.Formats
{
    /// <summary>
    /// A 4-bit Nintendo ADPCM audio stream.
    /// The stream can contain any number of individual channels.
    /// </summary>
    public class AdpcmFormat : AudioFormatBase
    {
        public AdpcmChannel[] Channels { get; }

        public List<AdpcmTrack> Tracks { get; set; }= new List<AdpcmTrack>();

        public AdpcmFormat(int sampleCount, int sampleRate, AdpcmChannel[] channels)
            : base(sampleCount, sampleRate, channels.Length)
        {
            Channels = channels;
        }

        public override Pcm16Format ToPcm16()
        {
            var a = new List<short[]>();
            foreach (AdpcmChannel channel in Channels)
            {
                a.Add(AdpcmDecoder.Decode(channel.AudioData, channel.Coefs, SampleCount, channel.Hist1, channel.Hist2));
            }

            return new Pcm16Format(SampleCount, SampleRate, a.ToArray());
        }
    }
}
