using VGAudio.Containers.Bxstm;

namespace VGAudio.Cli.Metadata
{
    internal static class Brstm
    {
        public static Common ToCommon(BrstmStructure structure)
        {
            return new Common
            {
                SampleCount = structure.SampleCount,
                SampleRate = structure.SampleRate,
                ChannelCount = structure.ChannelCount,
                Format = Formats.GcAdpcm,
                Looping = structure.Looping,
                LoopStart = structure.LoopStart,
                LoopEnd = structure.SampleCount
            };
        }
    }
}
