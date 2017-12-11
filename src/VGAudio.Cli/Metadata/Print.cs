using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VGAudio.Cli.Metadata.Containers;

namespace VGAudio.Cli.Metadata
{
    internal static class Print
    {
        public static string PrintMetadata(Options options)
        {
            AudioFile input = options.InFiles.First();
            string filename = input.Path;
            FileType type = input.Type;

            var metadataDisplay = new StringBuilder();

            MetadataReader reader = MetadataReaders[type];
            object metadata;

            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                metadata = reader.ReadMetadata(stream);
            }

            Common common = reader.ToCommon(metadata);
            PrintCommonMetadata(common, metadataDisplay);
            reader.PrintSpecificMetadata(metadata, metadataDisplay);

            return metadataDisplay.ToString();
        }

        public static void PrintCommonMetadata(Common common, StringBuilder builder)
        {
            builder.AppendLine($"Sample count: {common.SampleCount} {GetSecondsString(common.SampleCount, common.SampleRate)}");
            builder.AppendLine($"Sample rate: {common.SampleRate} Hz");
            builder.AppendLine($"Channel count: {common.ChannelCount}");
            builder.AppendLine($"Encoding format: {FormatDisplayNames[common.Format]}");

            if (common.Looping)
            {
                builder.AppendLine($"Loop start: {common.LoopStart} samples {GetSecondsString(common.LoopStart, common.SampleRate)}");
                builder.AppendLine($"Loop end: {common.LoopEnd} samples {GetSecondsString(common.LoopEnd, common.SampleRate)}");
            }
        }

        public static readonly Dictionary<AudioFormat, string> FormatDisplayNames = new Dictionary<AudioFormat, string>
        {
            [AudioFormat.Pcm16] = "16-bit PCM",
            [AudioFormat.Pcm8] = "8-bit PCM",
            [AudioFormat.GcAdpcm] = "GameCube \"DSP\" 4-bit ADPCM",
            [AudioFormat.ImaAdpcm] = "IMA 4-bit ADPCM",
            [AudioFormat.CriAdx] = "CRI ADX 4-bit ADPCM",
            [AudioFormat.CriAdxFixed] = "CRI ADX 4-bit ADPCM with fixed coefficients",
            [AudioFormat.CriAdxExp] = "CRI ADX 4-bit ADPCM with exponential scale",
            [AudioFormat.CriHca] = "CRI HCA",
            [AudioFormat.Atrac9] = "Sony ATRAC9"
        };

        public static readonly Dictionary<FileType, MetadataReader> MetadataReaders = new Dictionary<FileType, MetadataReader>
        {
            [FileType.Wave] = new Wave(),
            [FileType.Dsp] = new Dsp(),
            [FileType.Idsp] = new Idsp(),
            [FileType.Brstm] = new Brstm(),
            [FileType.Bcstm] = new Bcstm(),
            [FileType.Bfstm] = new Bfstm(),
            [FileType.Brwav] = new Brwav(),
            [FileType.Bcwav] = new Bcwav(),
            [FileType.Bfwav] = new Bfwav(),
            [FileType.Hps] = new Hps(),
            [FileType.Adx] = new Adx(),
            [FileType.Hca] = new Hca(),
            [FileType.Genh] = new Genh(),
            [FileType.Atrac9] = new Atrac9()
        };

        private static string GetSecondsString(int sampleCount, int sampleRate)
        {
            return $"({sampleCount / (double)sampleRate:0.0000} seconds)";
        }
    }
}
