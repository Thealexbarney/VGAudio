using System;
using System.Diagnostics;

namespace DspAdpcm.Cli
{
    public static class DspAdpcmCli
    {
        public static int Main(string[] args)
        {
            Options options = CliArguments.Parse(args);

            if (options.Job == JobType.Convert)
            {
                Stopwatch watch = Stopwatch.StartNew();
                bool success = Convert.ConvertFile(options);

                if (success)
                {
                    Console.WriteLine("Success!");
                    Console.WriteLine($"Time elapsed: {watch.Elapsed.TotalSeconds}");
                }
            }
            
            return 0;
        }
    }
}
