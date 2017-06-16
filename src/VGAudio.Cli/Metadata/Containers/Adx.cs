using System;
using System.IO;
using System.Text;
using VGAudio.Containers.Adx;
using VGAudio.Formats.CriAdx;

namespace VGAudio.Cli.Metadata.Containers
{
    internal class Adx : MetadataReader
    {
        public override object ReadMetadata(Stream stream)
        {
            return new AdxReader().ReadMetadata(stream);
        }

        public override Common ToCommon(object metadata)
        {
            var adx = metadata as AdxStructure;
            if (adx == null) throw new InvalidDataException("Could not parse file metadata.");

            return new Common
            {
                SampleCount = adx.SampleCount,
                SampleRate = adx.SampleRate,
                ChannelCount = adx.ChannelCount,
                Format = FromAdx(adx.EncodingType),
                Looping = adx.Looping,
                LoopStart = adx.LoopStartSample,
                LoopEnd = adx.LoopEndSample
            };
        }

        public override void PrintSpecificMetadata(object metadata, StringBuilder builder)
        {
            var adx = metadata as AdxStructure;
            if (adx == null) throw new InvalidDataException("Could not parse file metadata.");

            builder.AppendLine();

            builder.AppendLine($"Alignment samples: {adx.AlignmentSamples}");
            builder.AppendLine($"ADPCM frame size: {adx.FrameSize} bytes");
            builder.AppendLine($"File version: {adx.Version}");
            builder.AppendLine($"File version minor: {adx.VersionMinor}");
        }

        private static AudioFormat FromAdx(CriAdxType codec)
        {
            switch (codec)
            {
                case CriAdxType.Fixed:
                    return AudioFormat.CriAdxFixed;
                case CriAdxType.Linear:
                    return AudioFormat.CriAdx;
                case CriAdxType.Exponential:
                    return AudioFormat.CriAdxExp;
                default:
                    throw new ArgumentOutOfRangeException(nameof(codec), codec, null);
            }
        }
    }
}
