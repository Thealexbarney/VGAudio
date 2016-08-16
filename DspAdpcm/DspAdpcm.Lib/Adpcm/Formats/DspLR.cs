using System.IO;

namespace DspAdpcm.Lib.Adpcm.Formats
{
    internal class DspLR
    {
        public Dsp LeftChannel { get; }
        public Dsp RightChannel { get; }

        public DspLR(AdpcmStream stream)
        {
            if (stream.Channels.Count != 2)
            {
                throw new InvalidDataException($"Stream must have 2 channels, not {stream.Channels.Count}");
            }

            LeftChannel = new Dsp(stream.GetChannels(0, 1));
            RightChannel = new Dsp(stream.GetChannels(1, 1));
        }
    }
}
