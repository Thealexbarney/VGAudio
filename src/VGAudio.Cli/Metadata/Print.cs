using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VGAudio.Cli.Metadata.Containers;

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
            IMetadataReader reader = MetadataReaders[type];

            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                Metadata = reader.ReadMetadata(stream);
                Common = reader.ToCommon(Metadata);
            }
        }

        public void PrintCommonMetadata()
        {
            Console.WriteLine($"Sample count: {Common.SampleCount} {GetSecondsString(Common.SampleCount, Common.SampleRate)}");
            Console.WriteLine($"Sample rate: {Common.SampleRate} Hz");
            Console.WriteLine($"Channel count: {Common.ChannelCount}");
            Console.WriteLine($"Encoding format: {FormatDisplayNames[Common.Format]}");

            if (Common.Looping)
            {
                Console.WriteLine($"Loop start: {Common.LoopStart} samples {GetSecondsString(Common.LoopStart, Common.SampleRate)}");
                Console.WriteLine($"Loop end: {Common.LoopEnd} samples {GetSecondsString(Common.LoopEnd, Common.SampleRate)}");
            }
        }

        public static readonly Dictionary<AudioFormat, string> FormatDisplayNames = new Dictionary<AudioFormat, string>
        {
            [AudioFormat.Pcm16] = "16-bit PCM",
            [AudioFormat.Pcm8] = "8-bit PCM",
            [AudioFormat.GcAdpcm] = "GameCube \"DSP\" 4-bit ADPCM"
        };

        public static readonly Dictionary<FileType, IMetadataReader> MetadataReaders = new Dictionary<FileType, IMetadataReader>
        {
            [FileType.Dsp] = new Dsp(),
            [FileType.Brstm] = new Brstm()
        };

        private static string GetSecondsString(int sampleCount, int sampleRate)
        {
            return $"({sampleCount / (double)sampleRate:0.0000} seconds)";
        }
    }
}
