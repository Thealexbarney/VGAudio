using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace VGAudio.TestsLong.GcAdpcm
{
    internal static class Runner
    {
        public static void Run(FileType fileType, string audioPath, string dllPath)
        {
            var info = Common.FileTypes[fileType];
            var files = Directory.GetFiles(audioPath, info.Extension, SearchOption.AllDirectories);
            var watch = Stopwatch.StartNew();
            var encode = new Encode(files, new DspToolCafe64(dllPath), new DspToolVGAudio(), info.GetReader);

             encode.Run().ForAll(x => Console.WriteLine(PrintResult(x)));

            watch.Stop();
            Console.WriteLine(watch.Elapsed.TotalMilliseconds);
        }

        public static void Run(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("Usage: GcAdpcm <audio file type> <audio file path> <dsptool dlls path>");
                return;
            }

            FileType fileType = Parse.ParseFileType(args[1]);
            if (fileType == FileType.NotSet) return;

            Run(fileType, args[2], args[3]);
        }

        public static string PrintResult(Result result)
        {
            var builder = new StringBuilder();
            if (!result.Equal || result.RanFineComparison) builder.AppendLine();
            builder.Append($"Channel {result.Channel}; {result.Filename}: ");

            if (result.Equal && !result.RanFineComparison)
            {
                builder.Append("Equal");
                return builder.ToString();
            }

            builder.AppendLine();

            if (result.Exception != null)
            {
                builder.AppendLine("Exception thrown:");
                builder.AppendLine(result.Exception.ToString());
                return builder.ToString();
            }

            if (result.Equal)
            {
                builder.AppendLine("Coarse comparison failed but fine comparison was equal");
                return builder.ToString();
            }

            if (!result.CoefsEqual)
            {
                builder.AppendLine("Coefficients not equal");
                builder.AppendLine(PrintShort(result.CoefsA, "CoefsA"));
                builder.AppendLine(PrintShort(result.CoefsB, "CoefsB"));
                return builder.ToString();
            }

            builder.AppendLine("Encode not equal");
            builder.AppendLine($"Frame {result.Frame}; Frame Sample {result.FrameSample}; Sample {result.Sample}");
            builder.AppendLine(PrintShort(result.CoefsA, "Coefs"));
            builder.AppendLine(PrintShort(result.PcmIn, "PcmIn"));
            builder.AppendLine(PrintShort(result.PcmOutA, "DecodedPcmA          "));
            builder.AppendLine(PrintShort(result.PcmOutB, "DecodedPcmB          "));

            builder.AppendLine(PrintByte(result.AdpcmOutA, "EncodedAdpcmA"));
            builder.AppendLine(PrintByte(result.AdpcmOutB, "EncodedAdpcmB"));
            return builder.ToString();
        }

        private static string PrintShort(short[] data, string name)
        {
            StringBuilder stringOut = new StringBuilder($"short[] {name} = {{ ");

            for (int i = 0; i < data.Length; i++)
            {
                if (i != 0)
                {
                    stringOut.Append(", ");
                }
                stringOut.AppendFormat($"0x{data[i]:X4}");
            }
            stringOut.Append(" };");
            return stringOut.ToString();
        }
        private static string PrintByte(byte[] data, string name)
        {
            StringBuilder stringOut = new StringBuilder($"byte[] {name} = {{ ");

            for (int i = 0; i < data.Length; i++)
            {
                if (i != 0)
                {
                    stringOut.Append(", ");
                }
                stringOut.AppendFormat($"0x{data[i]:X2}");
            }
            stringOut.Append(" };");
            return stringOut.ToString();
        }
    }
}