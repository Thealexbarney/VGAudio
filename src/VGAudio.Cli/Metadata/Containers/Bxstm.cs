using System.IO;
using VGAudio.Containers.Bxstm;

namespace VGAudio.Cli.Metadata.Containers
{
    internal static class Bxstm
    {
        public static Common ToCommon(object structure)
        {
            var bxstm = structure as BxstmStructure;
            if (bxstm == null) throw new InvalidDataException("Could not parse file metadata.");

            return new Common
            {
                SampleCount = bxstm.SampleCount,
                SampleRate = bxstm.SampleRate,
                ChannelCount = bxstm.ChannelCount,
                Format = AudioFormat.GcAdpcm,
                Looping = bxstm.Looping,
                LoopStart = bxstm.LoopStart,
                LoopEnd = bxstm.SampleCount
            };
        }
    }
}
