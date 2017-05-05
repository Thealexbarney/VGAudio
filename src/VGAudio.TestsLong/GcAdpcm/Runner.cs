using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace VGAudio.TestsLong.GcAdpcm
{
    public static class Runner
    {
        public static string Run(string audioPath, string dllPath)
        {
            var waves = Directory.GetFiles(audioPath, "*.wav");
            var watch = Stopwatch.StartNew();
            var encode = new Encode(new DspToolRevolution(dllPath), new DspToolVGAudio());
            var results = waves.AsParallel().SelectMany(x => encode.CompareWave(x));

            results.ForAll(PrintResult);

            watch.Stop();
            Console.WriteLine(watch.Elapsed.TotalMilliseconds);

            return "";
        }

        private static void PrintResult(Result result)
        {
            if (result.Equal) Console.WriteLine();
            Console.Write($"Channel {result.Channel}; {result.Filename}: ");

            if (result.Equal)
            {
                Console.WriteLine("Equal");
                return;
            }
            Console.WriteLine();

            if (!result.CoefsEqual)
            {
                Console.WriteLine("Coefficients not equal");
                Console.WriteLine(PrintShort(result.CoefsA, "CoefsA"));
                Console.WriteLine(PrintShort(result.CoefsB, "CoefsB"));
                return;
            }

            Console.WriteLine("Encode not equal");
            Console.WriteLine($"Frame {result.Frame}; Frame Sample {result.FrameSample}; Sample {result.Sample}");
            Console.WriteLine(PrintShort(result.CoefsA, "Coefs"));
            Console.WriteLine(PrintShort(result.PcmIn, "PcmIn"));
            Console.WriteLine(PrintShort(result.PcmOutA, "DecodedPcmA          "));
            Console.WriteLine(PrintShort(result.PcmOutB, "DecodedPcmB          "));

            Console.WriteLine(PrintByte(result.AdpcmOutA, "EncodedAdpcmA"));
            Console.WriteLine(PrintByte(result.AdpcmOutB, "EncodedAdpcmB"));
            Console.WriteLine();
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