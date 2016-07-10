using System;
using System.Diagnostics;
using System.IO;
using DspAdpcm.Encode.Adpcm;
using DspAdpcm.Encode.Wave;

namespace DspAdpcm.Cli
{
    public static class DspAdpcmCli
    {
        public static int Main(string[] args)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            if (args.Length < 2)
            {
                Console.WriteLine("Usage: dspenc <wavin> <dspout>\n");
                return 0;
            }

            WaveStream wave;

            try
            {
                using (var file = new FileStream(args[0], FileMode.Open))
                {
                    wave = new WaveStream(file);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return -1;
            }

            AdpcmStream adpcm = new AdpcmStream(wave);
            adpcm.Encode();

            watch.Stop();
            Console.WriteLine($"DONE! {adpcm.NumSamples} samples processed\n");
            Console.WriteLine($"Time elapsed: {watch.Elapsed.TotalSeconds}");

            using (var stream = File.Open(args[1], FileMode.Create))
                foreach (var b in adpcm.GetDspFile())
                    stream.WriteByte(b);

            return 0;
        }
    }
}
