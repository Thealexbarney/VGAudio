using System;

namespace VGAudio.Tools.CrackAdx
{
    internal static class Runner
    {
        public static void Run(string path) => Run(path, null);

        public static void Run(string path, string executable)
        {
            using (var progress = new ProgressBar())
            {
                var guess = new GuessAdx(path, executable, progress);
                guess.Run();
            }
        }

        public static void Run(string[] args)
        {
            if (args.Length == 2)
            {
                Run(args[1]);
                return;
            }

            if (args.Length == 3)
            {
                Run(args[1], args[2]);
                return;
            }

            Console.WriteLine("Usage: AdxCrack <adx file or directory of files> [game executable (for encryption type 8)]");
        }
    }
}
