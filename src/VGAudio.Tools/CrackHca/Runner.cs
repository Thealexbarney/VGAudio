using System;

namespace VGAudio.Tools.CrackHca
{
    internal static class Runner
    {
        public static void Run(string path)
        {
            using (var progress = new ProgressBar())
            {
                var guess = new Crack(path, progress);
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

            Console.WriteLine("Usage: CrackHca <directory of HCA files>");
        }
    }
}
