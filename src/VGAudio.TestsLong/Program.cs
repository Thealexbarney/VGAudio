using System;
using VGAudio.TestsLong.Rebuild;

namespace VGAudio.TestsLong
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: <path> <file type index>");
                return;
            }

            if (!int.TryParse(args[1], out int index))
            {
                Console.WriteLine($"Couldn't parse {args[1]} as an integer");
                return;
            }

            string results = Runner.Run(args[0], index);

            Console.WriteLine(results);
        }
    }
}