using VGAudio.Codecs.GcAdpcm;
using VGAudio.Formats.GcAdpcm;

namespace VGAudio.Containers.Hps
{
    public class HpsChannelInfo : GcAdpcmChannelInfo
    {
        public int MaxBlockSize { get; set; }
        public int EndAddress { get; set; }
        public int SampleCount => GcAdpcmMath.NibbleToSample(EndAddress) + 1;
    }
}
