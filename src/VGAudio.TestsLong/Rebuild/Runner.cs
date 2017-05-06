using System;
using System.Linq;
using System.Text;
using VGAudio.Containers;

namespace VGAudio.TestsLong.Rebuild
{
    internal static class Runner
    {
        private static Result[] Dsp(string path) =>
            new Rebuilder(path, "*.dsp", () => new DspReader(), () => new DspWriter()).Run();

        private static Result[] Idsp(string path) =>
            new Rebuilder(path, "*.idsp", () => new IdspReader(), () => new IdspWriter()).Run();

        private static Result[] Brstm(string path) =>
            new Rebuilder(path, "*.brstm", () => new BrstmReader(), () => new BrstmWriter()).Run();

        private static Result[] Bcstm(string path) =>
            new Rebuilder(path, "*.bcstm", () => new BcstmReader(), () => new BcstmWriter()).Run();

        private static Result[] Bfstm(string path) =>
            new Rebuilder(path, "*.bfstm", () => new BfstmReader(), () => new BfstmWriter()).Run();

        public static string Run(string path, FileType fileType)
        {
            Result[] results;
            switch (fileType)
            {
                case FileType.Dsp:
                    results = Dsp(path);
                    break;
                case FileType.Idsp:
                    results = Idsp(path);
                    break;
                case FileType.Brstm:
                    results = Brstm(path);
                    break;
                case FileType.Bcstm:
                    results = Bcstm(path);
                    break;
                case FileType.Bfstm:
                    results = Bfstm(path);
                    break;
                default:
                    return "Rebuilding not implemented for this type";
            }

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

            string results = Run(args[2], fileType);

            Console.WriteLine(results);
        }
    }
}
