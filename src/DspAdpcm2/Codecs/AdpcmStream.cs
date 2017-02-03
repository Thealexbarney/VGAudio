using System.Collections.Generic;

namespace DspAdpcm.Codecs
{
    /// <summary>
    /// A 4-bit Nintendo ADPCM audio stream.
    /// The stream can contain any number of individual channels.
    /// </summary>
    public class AdpcmStream
    {
        public int SampleCount { get; }

        internal List<AdpcmChannel> Channels { get; } = new List<AdpcmChannel>();

        public List<AdpcmTrack> Tracks { get; set; }= new List<AdpcmTrack>();

        public AdpcmChannel[] GetAudio => Channels.ToArray();

        public AdpcmStream(int sampleCount)
        {
            SampleCount = sampleCount;
        }

        internal bool AddChannel(AdpcmChannel audio)
        {
            if (audio.SampleCount != SampleCount)
            {
                return false;
            }
            Channels.Add(audio);

            return true;
        }
    }
}
