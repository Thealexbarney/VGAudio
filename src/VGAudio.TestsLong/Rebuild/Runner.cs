using System.Linq;
using System.Text;
using VGAudio.Containers;

namespace VGAudio.TestsLong.Rebuild
{
    internal static class Runner
    {
        public static Result[] Dsp(string path) =>
            new Rebuilder(path, "*.dsp", () => new DspReader(), () => new DspWriter()).Run();

        public static Result[] Idsp(string path) =>
            new Rebuilder(path, "*.idsp", () => new IdspReader(), () => new IdspWriter()).Run();

        public static Result[] Brstm(string path) =>
            new Rebuilder(path, "*.brstm", () => new BrstmReader(), () => new BrstmWriter()).Run();

        public static Result[] Bcstm(string path) =>
            new Rebuilder(path, "*.bcstm", () => new BcstmReader(), () => new BcstmWriter()).Run();

        public static Result[] Bfstm(string path) =>
            new Rebuilder(path, "*.bfstm", () => new BfstmReader(), () => new BfstmWriter()).Run();

        public static string Run(string path, int index)
        {
            Result[] results;
            switch (index)
            {
                case 0:
                    results = Dsp(path);
                    break;
                case 1:
                    results = Idsp(path);
                    break;
                case 2:
                    results = Brstm(path);
                    break;
                case 3:
                    results = Bcstm(path);
                    break;
                case 4:
                    results = Bfstm(path);
                    break;
                default:
                    return "Index is not in range";
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
    }
}
