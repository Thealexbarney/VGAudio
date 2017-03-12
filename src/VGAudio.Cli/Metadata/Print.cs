using System;
using System.IO;
using System.Linq;
using VGAudio.Containers;
using VGAudio.Containers.Bxstm;
using VGAudio.Containers.Dsp;

namespace VGAudio.Cli.Metadata
{
    internal class Print
    {
        public Common Common { get; set; }
        public object Metadata { get; set; }

        public void GetMetadata(Options options)
        {
            AudioFile input = options.InFiles.First();

            string filename = input.Path;
            FileType type = input.Type;

            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                switch (type)
                {
                    case FileType.Dsp:
                        Metadata = new DspReader().ReadMetadata(stream);
                        Common = Dsp.ToCommon((DspStructure) Metadata);
                        break;
                    case FileType.Brstm:
                        Metadata = new BrstmReader().ReadMetadata(stream);
                        Common = Brstm.ToCommon((BrstmStructure) Metadata);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public void PrintCommonMetadata()
        {
            Console.WriteLine($"Sample count: {Common.SampleCount} {GetSecondsString(Common.SampleCount, Common.SampleRate)}");
            Console.WriteLine($"Sample rate: {Common.SampleRate} Hz");
            Console.WriteLine($"Channel count: {Common.ChannelCount}");
            Console.WriteLine($"Encoding format: {FormatsDictionary.Display[Common.Format]}");

            if (Common.Looping)
            {
                Console.WriteLine($"Loop start: {Common.LoopStart} samples {GetSecondsString(Common.LoopStart, Common.SampleRate)}");
                Console.WriteLine($"Loop end: {Common.LoopEnd} samples {GetSecondsString(Common.LoopEnd, Common.SampleRate)}");
            }
        }

        private static string GetSecondsString(int sampleCount, int sampleRate)
        {
            return $"({sampleCount / (double)sampleRate:#.0000} seconds)";
        }
    }
}
