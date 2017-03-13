using System.IO;
using System.Text;
using VGAudio.Containers.Bxstm;
using VGAudio.Formats.GcAdpcm;

#if NET20
using VGAudio.Compatibility.LinqBridge;
#else
using System.Linq;
#endif

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

        public static void PrintSpecificMetadata(object structure, StringBuilder builder)
        {
            var bxstm = structure as BxstmStructure;
            if (bxstm == null) throw new InvalidDataException("Could not parse file metadata.");

            GcAdpcm.PrintAdpcmMetadata(bxstm.Channels.Cast<GcAdpcmChannelInfo>().ToList(), builder);
        }
    }
}
