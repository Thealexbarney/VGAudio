using System;
using System.Linq;
using System.Text;

namespace VGAudio.Tools.Rebuild
{
    internal static class Runner
    {
        public static string Run(FileType fileType, string path)
        {
            var info = Common.FileTypes[fileType];
            Result[] results = new Rebuilder(path, info.Extension, info.GetReader, info.GetWriter).Run();

            if (results.Length == 0)
                return "No files found";

            var builder = new StringBuilder();
            builder.AppendLine($"Same: {results.Count(x => x.ByteCount == 0)}");
            builder.AppendLine($"Different: {results.Count(x => x.ByteCount > 0)}");
            builder.AppendLine($"Different size: {results.Count(x => x.ByteCount == -2)}");
            builder.AppendLine($"Error: {results.Count(x => x.ByteCount == -4)}");

            foreach (Result result in results.OrderByDescending(x => x.ByteCount))
            {
                builder.AppendLine($"{result.ByteCount}, {result.Filename}");
            }

            builder.AppendLine($"Same: {results.Count(x => x.ByteCount == 0)}");
            builder.AppendLine($"Different: {results.Count(x => x.ByteCount > 0)}");
            builder.AppendLine($"Different size: {results.Count(x => x.ByteCount == -2)}");
            builder.AppendLine($"Error: {results.Count(x => x.ByteCount == -4)}");
            return builder.ToString();
        }

        public static void Run(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: Rebuild <file type> <path>");
                return;
            }

            FileType fileType = Parse.ParseFileType(args[1]);
            if (fileType == FileType.NotSet) return;

            string results = Run(fileType, args[2]);

            Console.WriteLine(results);
        }
    }
}
