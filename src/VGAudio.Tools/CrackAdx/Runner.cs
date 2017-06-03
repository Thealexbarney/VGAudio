using System;

namespace VGAudio.Tools.CrackAdx
{
    internal static class Runner
    {
        public static void Run(string path)
        {
            using (var progress = new ProgressBar())
            {
                var guess = new GuessAdx(path, progress);
                guess.Run();
            }
        }

        public static void Run(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: AdxCrack <path>");
                return;
            }

            Run(args[1]);
        }
    }
}
