using System.IO;
using System.Linq;
using System.Text;
using VGAudio.Containers.Bxstm;
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

            return new Common
            {
                SampleCount = bxstm.SampleCount,
                SampleRate = bxstm.SampleRate,
                ChannelCount = bxstm.ChannelCount,
                Format = Common.FromBxstm(bxstm.Codec),
                Looping = bxstm.Looping,
                LoopStart = bxstm.LoopStart,
                LoopEnd = bxstm.SampleCount
            };
        }

        public static void PrintSpecificMetadata(object structure, StringBuilder builder)
        {
            var bxstm = structure as BxstmStructure;
            if (bxstm == null) throw new InvalidDataException("Could not parse file metadata.");

            builder.AppendLine();

            var calculatedSamples = (bxstm.InterleaveCount - 1) * bxstm.SamplesPerInterleave +
                                    GcAdpcmHelpers.NibbleCountToSampleCount(bxstm.LastBlockSizeWithoutPadding * 2);

            builder.AppendLine($"Interleave Count: {bxstm.InterleaveCount}");
            builder.AppendLine($"Interleave Size: 0x{bxstm.InterleaveSize:X}");
            builder.AppendLine($"Samples per interleave: {bxstm.SamplesPerInterleave}");
            builder.AppendLine($"Last interleave size without padding: 0x{bxstm.LastBlockSizeWithoutPadding:X}");
            builder.AppendLine($"Last interleave size: 0x{bxstm.LastBlockSize:X}");
            builder.AppendLine($"Samples in last interleave block: {bxstm.LastBlockSamples}");
            builder.AppendLine($"Sample count from data size: {calculatedSamples}");
            builder.AppendLine();

            builder.AppendLine($"Samples per seek table entry: {bxstm.SamplesPerSeekTableEntry}");
            
            for (int i = 0; i < bxstm.Tracks.Count; i++)
            {
                builder.AppendLine();
                builder.AppendLine($"Track {i}");
                builder.AppendLine(new string('-', 25));
                PrintTrackMetadata(bxstm.Tracks[i], builder);
            }

            if (bxstm.Codec != BxstmCodec.Adpcm) return;
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
    }
}
