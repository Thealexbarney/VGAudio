using System.IO;
using System.Text;
using VGAudio.Containers.Hca;

namespace VGAudio.Cli.Metadata.Containers
{
    internal class Hca : MetadataReader
    {
        public override object ReadMetadata(Stream stream)
        {
            return new HcaReader().ReadMetadata(stream);
        }

        public override Common ToCommon(object metadata)
        {
            var hcaStructure = metadata as HcaStructure;
            var hca = hcaStructure?.Hca;
            if (hcaStructure == null || hca == null) throw new InvalidDataException("Could not parse file metadata.");

            return new Common
            {
                SampleCount = hca.SampleCount,
                SampleRate = hca.SampleRate,
                ChannelCount = hca.ChannelCount,
                Format = AudioFormat.CriHca,
                Looping = hca.Looping,
                LoopStart = hca.LoopStartSample,
                LoopEnd = hca.LoopEndSample
            };
        }

        public override void PrintSpecificMetadata(object metadata, StringBuilder builder)
        {
            var hcaStructure = metadata as HcaStructure;
            var hca = hcaStructure?.Hca;
            if (hcaStructure == null || hca == null) throw new InvalidDataException("Could not parse file metadata.");

            builder.AppendLine();

            builder.AppendLine($"HCA version: {hcaStructure.Version >> 8}.{hcaStructure.Version & 0xf}");
            builder.AppendLine($"Frame size: {hca.FrameSize} bytes");
            builder.AppendLine($"Frame count: {hca.FrameCount}");
            builder.AppendLine($"Inserted samples: {hca.InsertedSamples}");
            builder.AppendLine($"Base band count: {hca.BaseBandCount}");
            builder.AppendLine($"Joint stereo band count: {hca.StereoBandCount}");
            builder.AppendLine($"HFR band count: {hca.HfrBandCount}");
            builder.AppendLine($"Total band count: {hca.TotalBandCount}");
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (hca.Volume != 1) builder.AppendLine($"Volume: {hca.Volume}");
            if (!string.IsNullOrWhiteSpace(hca.Comment)) builder.AppendLine($"Comment: {hca.Comment}");
        }
    }
}
