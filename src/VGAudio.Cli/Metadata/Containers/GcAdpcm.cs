using System.Text;
using VGAudio.Formats.GcAdpcm;

namespace VGAudio.Cli.Metadata.Containers
{
    internal static class GcAdpcm
    {
        public static void PrintAdpcmMetadata(GcAdpcmChannelInfo channel, StringBuilder builder)
        {
            builder.AppendLine("Coefficients:");
            builder.Append($"{CoefficientsToString(channel.Coefs)}");
        }

        public static string CoefficientsToString(short[] coefs)
        {
            if (coefs.Length < 14) return "";

            var coefOut = new StringBuilder();

            for (int i = 0; i < 7; i++)
            {
                coefOut.AppendLine($"0x{coefs[i * 2]:X4}, 0x{coefs[i * 2 + 1]:X4}");
            }
            return coefOut.ToString();
        }
    }
}
