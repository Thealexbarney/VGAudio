using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace VGAudio.Tools.Atrac9
{
    internal static class Runner
    {
        public static void Run(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: Atrac9 <task>");
                Console.WriteLine("Available tasks: Decode, BatchEncode");
                return;
            }

            switch (args[1].ToLower())
            {
                case "decode":
                    RunDecode(args);
                    break;
                case "batchencode":
                    RunBatchEncode(args);
                    break;
                default:
                    Console.WriteLine("Usage: Atrac9 <task>");
                    Console.WriteLine("Available tasks: Decode, BatchEncode");
                    break;
            }
        }

        public static void RunDecode(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("Usage: Atrac9 decode <audio file path> <at9 tools path>");
                return;
            }
            var audioPath = args[2];
            var exePath = args[3];

            var info = Common.FileTypes[FileType.Atrac9];
            var files = Directory.GetFiles(audioPath, info.Extension, SearchOption.AllDirectories);
            var watch = Stopwatch.StartNew();
            var decode = new Decode(files, new At9ToolVGAudio(), new At9ToolExe(exePath));

            decode.Run().ForAll(x => Console.WriteLine(PrintResult(x)));

            watch.Stop();
            Console.WriteLine(watch.Elapsed.TotalMilliseconds);
        }

        public static void RunBatchEncode(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("Usage: Atrac9 decode <file path> <file name>");
                return;
            }

            var directory = Path.GetFullPath(args[2]);
            var fileName = Path.Combine(directory, args[3]);
            var at9ToolPath = Path.Combine(directory, "at9tool.exe");

            if (!Directory.Exists(directory))
            {
                Console.WriteLine($"Can't find directory {directory}");
                return;
            }
            if (!File.Exists(fileName))
            {
                Console.WriteLine($"Can't find file {fileName}.");
                return;
            }
            if (!File.Exists(at9ToolPath))
            {
                Console.WriteLine($"Can't find file {at9ToolPath}.");
                return;
            }

            using (var progress = new ProgressBar())
            {
                var encode = new BatchEncode(directory, fileName, at9ToolPath);
                encode.Run(progress);
            }
        }

        public static string PrintResult(Result result)
        {
            var builder = new StringBuilder();
            if (!result.Equal) builder.AppendLine();
            builder.Append($"{result.Filename}: ");

            if (result.Equal)
            {
                builder.Append("Equal");
                return builder.ToString();
            }

            if (result.Invalid)
            {
                builder.Append("Invalid File");
                return builder.ToString();
            }

            builder.AppendLine();

            if (result.Exception != null)
            {
                builder.AppendLine("Exception thrown:");
                builder.AppendLine(result.Exception.ToString());
                return builder.ToString();
            }

            if (result.Sample == -3)
            {
                builder.AppendLine("Audio sizes different");
                return builder.ToString();
            }

            builder.AppendLine("Encode not equal");
            builder.AppendLine($"Channel {result.Channel}; Frame {result.Frame}; Frame Sample {result.FrameSample}; Sample {result.Sample}");
            return builder.ToString();
        }
    }
}
