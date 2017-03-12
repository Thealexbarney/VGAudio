using VGAudio.Containers.Dsp;

namespace VGAudio.Cli.Metadata
{
    internal static class Dsp
    {
        public static Common ToCommon(DspStructure structure)
        {
            return new Common
            {
                SampleCount = structure.SampleCount,
                SampleRate = structure.SampleRate,
                ChannelCount = structure.ChannelCount,
                Format = Formats.GcAdpcm,
                Looping = structure.Looping,
                LoopStart = structure.LoopStart,
                LoopEnd = structure.LoopEnd
            };
        }
    }
}
