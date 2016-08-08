using System.IO;

namespace DspAdpcm.Encode.Adpcm.Formats
{
    public class DspLR
    {
        public Dsp LeftChannel { get; }
        public Dsp RightChannel { get; }

        public DspLR(AdpcmStream stream)
        {
            if (stream.Channels.Count != 2)
            {
                throw new InvalidDataException($"Stream must have 2 channels, not {stream.Channels.Count}");
            }

            LeftChannel = new Dsp(stream.ShallowCloneChannelSubset(0, 0));
            RightChannel = new Dsp(stream.ShallowCloneChannelSubset(1, 1));
        }
    }
}
