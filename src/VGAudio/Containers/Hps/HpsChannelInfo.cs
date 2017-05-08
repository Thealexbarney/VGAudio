using VGAudio.Formats.GcAdpcm;

namespace VGAudio.Containers.Hps
{
    public class HpsChannelInfo : GcAdpcmChannelInfo
    {
        public int MaxBlockSize { get; set; }
        public int NibbleCount { get; set; }
        public int SampleCount => GcAdpcmHelpers.NibbleCountToSampleCount(NibbleCount);
    }
}
