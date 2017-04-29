using System.Collections.Generic;
using System.Text;
using VGAudio.Formats.GcAdpcm;

namespace VGAudio.Cli.Metadata.Containers
{
    internal static class GcAdpcm
    {
        public static void PrintAdpcmMetadata(IList<GcAdpcmChannelInfo> channels, StringBuilder builder)
        {
            for (int i = 0; i < channels.Count; i++)
            {
                builder.AppendLine($"\nChannel {i}");
                builder.AppendLine(new string('-', 40));
                PrintChannelMetadata(channels[i], builder);
            }
        }

        public static void PrintChannelMetadata(GcAdpcmChannelInfo channel, StringBuilder builder)
        {
            builder.AppendLine("Coefficients:");
            builder.AppendLine($"{CoefficientsToString(channel.Coefs)}");
            builder.AppendLine($"Gain: 0x{channel.Gain:X4}");
            builder.AppendLine($"Pred/Scale: 0x{channel.PredScale:X4}");
            builder.AppendLine($"History sample 1 (n-1): 0x{channel.Hist1:X4}");
            builder.AppendLine($"History sample 2 (n-2): 0x{channel.Hist2:X4}");
            builder.AppendLine();
            builder.AppendLine($"Loop Pred/Scale: 0x{channel.LoopPredScale:X4}");
            builder.AppendLine($"Loop History sample 1: 0x{channel.LoopHist1:X4}");
            builder.AppendLine($"Loop History sample 2: 0x{channel.LoopHist1:X4}");
        }

        public static string CoefficientsToString(short[] coefs)
        {
            if (coefs == null || coefs.Length < 14) return "";

            var coefOut = new StringBuilder();

            for (int i = 0; i < 7; i++)
            {
                coefOut.AppendLine($"[{i}]: 0x{coefs[i * 2]:X4}, 0x{coefs[i * 2 + 1]:X4}");
            }
            return coefOut.ToString();
        }
    }
}
