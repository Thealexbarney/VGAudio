using System;
using System.IO;
using System.Linq;
using System.Text;
using VGAudio.Containers.Bxstm;
using VGAudio.Containers.Bxstm.Structures;
using VGAudio.Formats;
using VGAudio.Formats.GcAdpcm;

namespace VGAudio.Cli.Metadata.Containers
{
    internal static class Bxstm
    {
        public static Common ToCommon(object structure)
        {
            var bxstm = structure as BxstmStructure;
            if (bxstm == null) throw new InvalidDataException("Could not parse file metadata.");
            StreamInfo info = bxstm.StreamInfo;

            return new Common
            {
                SampleCount = info.SampleCount,
                SampleRate = info.SampleRate,
                ChannelCount = info.ChannelCount,
                Format = FromBxstm(info.Codec),
                Looping = info.Looping,
                LoopStart = info.LoopStart,
                LoopEnd = info.SampleCount
            };
        }

        public static void PrintSpecificMetadata(object structure, StringBuilder builder)
        {
            var bxstm = structure as BxstmStructure;
            if (bxstm == null) throw new InvalidDataException("Could not parse file metadata.");
            StreamInfo info = bxstm.StreamInfo;

            builder.AppendLine();

            var calculatedSamples = (info.InterleaveCount - 1) * info.SamplesPerInterleave +
                                    GcAdpcmHelpers.NibbleCountToSampleCount(info.LastBlockSizeWithoutPadding * 2);

            builder.AppendLine($"Interleave Count: {info.InterleaveCount}");
            builder.AppendLine($"Interleave Size: 0x{info.InterleaveSize:X}");
            builder.AppendLine($"Samples per interleave: {info.SamplesPerInterleave}");
            builder.AppendLine($"Last interleave size without padding: 0x{info.LastBlockSizeWithoutPadding:X}");
            builder.AppendLine($"Last interleave size: 0x{info.LastBlockSize:X}");
            builder.AppendLine($"Samples in last interleave block: {info.LastBlockSamples}");
            builder.AppendLine($"Sample count from data size: {calculatedSamples}");
            builder.AppendLine();

            builder.AppendLine($"Samples per seek table entry: {info.SamplesPerSeekTableEntry}");

            for (int i = 0; i < bxstm.Tracks.Count; i++)
            {
                builder.AppendLine();
                builder.AppendLine($"Track {i}");
                builder.AppendLine(new string('-', 25));
                PrintTrackMetadata(bxstm.Tracks[i], builder);
            }

            if (info.Codec != BxstmCodec.Adpcm) return;
            GcAdpcm.PrintAdpcmMetadata(bxstm.Channels.Cast<GcAdpcmChannelInfo>().ToList(), builder);
        }

        public static void PrintTrackMetadata(AudioTrack track, StringBuilder builder)
        {
            builder.AppendLine($"Channel Count: {track.ChannelCount}");
            builder.AppendLine($"Left channel ID: {track.ChannelLeft}");
            builder.AppendLine($"Right channel ID: {track.ChannelRight}");
            builder.AppendLine($"Volume: 0x{track.Volume:X2}");
            builder.AppendLine($"Panning: 0x{track.Panning:X2}");
        }

        public static AudioFormat FromBxstm(BxstmCodec codec)
        {
            switch (codec)
            {
                case BxstmCodec.Pcm8Bit:
                    return AudioFormat.Pcm8;
                case BxstmCodec.Pcm16Bit:
                    return AudioFormat.Pcm16;
                case BxstmCodec.Adpcm:
                    return AudioFormat.GcAdpcm;
                default:
                    throw new ArgumentOutOfRangeException(nameof(codec), codec, null);
            }
        }
    }
}
