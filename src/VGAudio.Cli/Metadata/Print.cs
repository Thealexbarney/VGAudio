using System;
using System.Collections.Generic;
using System.IO;
using VGAudio.Cli.Metadata.Containers;

#if NET20
using VGAudio.Compatibility.LinqBridge;
#else
using System.Linq;
#endif

namespace VGAudio.Cli.Metadata
{
    internal static class Print
    {
        public static void PrintMetadata(Options options)
        {
            AudioFile input = options.InFiles.First();
            string filename = input.Path;
            FileType type = input.Type;

            MetadataReader reader = MetadataReaders[type];
            object metadata;

            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                metadata = reader.ReadMetadata(stream);
            }

            Common common = reader.ToCommon(metadata);
            PrintCommonMetadata(common);
            reader.PrintSpecificMetadata(metadata);
        }

        public static void PrintCommonMetadata(Common common)
        {
            Console.WriteLine($"Sample count: {common.SampleCount} {GetSecondsString(common.SampleCount, common.SampleRate)}");
            Console.WriteLine($"Sample rate: {common.SampleRate} Hz");
            Console.WriteLine($"Channel count: {common.ChannelCount}");
            Console.WriteLine($"Encoding format: {FormatDisplayNames[common.Format]}");

            if (common.Looping)
            {
                Console.WriteLine($"Loop start: {common.LoopStart} samples {GetSecondsString(common.LoopStart, common.SampleRate)}");
                Console.WriteLine($"Loop end: {common.LoopEnd} samples {GetSecondsString(common.LoopEnd, common.SampleRate)}");
            }
        }

        public static readonly Dictionary<AudioFormat, string> FormatDisplayNames = new Dictionary<AudioFormat, string>
        {
            [AudioFormat.Pcm16] = "16-bit PCM",
            [AudioFormat.Pcm8] = "8-bit PCM",
            [AudioFormat.GcAdpcm] = "GameCube \"DSP\" 4-bit ADPCM"
        };

        public static readonly Dictionary<FileType, MetadataReader> MetadataReaders = new Dictionary<FileType, MetadataReader>
        {
            [FileType.Wave] = new Wave(),
            [FileType.Dsp] = new Dsp(),
            [FileType.Idsp] = new Idsp(),
            [FileType.Brstm] = new Brstm(),
            [FileType.Bcstm] = new Bcstm(),
            [FileType.Bfstm] = new Bfstm(),
            [FileType.Genh] = new Genh()
        };

        private static string GetSecondsString(int sampleCount, int sampleRate)
        {
            return $"({sampleCount / (double)sampleRate:0.0000} seconds)";
        }
    }
}
