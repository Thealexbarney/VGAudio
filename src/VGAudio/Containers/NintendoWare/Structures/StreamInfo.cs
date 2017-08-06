using System.IO;

namespace VGAudio.Containers.NintendoWare.Structures
{
    public class StreamInfo
    {
        /// <summary>The audio codec.</summary>
        public NwCodec Codec { get; set; }
        /// <summary>This flag is set if the file loops.</summary>
        public bool Looping { get; set; }
        /// <summary>The number of channels in the file.</summary>
        public int ChannelCount { get; set; }
        /// <summary>The number of audio regions in the file.</summary>
        public int RegionCount { get; set; }
        /// <summary>The sample rate of the audio.</summary>
        public int SampleRate { get; set; }
        /// <summary>The start loop point in samples.</summary>
        public int LoopStart { get; set; }
        /// <summary>The number of samples in the file.</summary>
        public int SampleCount { get; set; }

        /// <summary>The total count of interleaved audio data blocks.</summary>
        public int InterleaveCount { get; set; }
        /// <summary>
        /// The number of bytes per channel in each
        /// interleaved audio data block.
        /// </summary>
        public int InterleaveSize { get; set; }
        /// <summary>
        /// The number of samples per channel in each
        /// interleaved audio data block.
        /// </summary>
        public int SamplesPerInterleave { get; set; }
        /// <summary>
        /// The number of bytes per channel in the final
        /// interleaved audio data block, not including
        /// the padding at the end of each channel.
        /// </summary>
        public int LastBlockSizeWithoutPadding { get; set; }
        /// <summary>
        /// The number of samples per channel in the final
        /// interleaved audio data block.
        /// </summary>
        public int LastBlockSamples { get; set; }
        /// <summary>
        /// The number of bytes per channel in the final
        /// interleaved audio data block, including
        /// the padding at the end of each channel.
        /// </summary>
        public int LastBlockSize { get; set; }
        /// <summary>The number of bytes per seek table entry.</summary>
        public int BytesPerSeekTableEntry { get; set; }
        /// <summary>The number of samples per seek table entry.</summary>
        public int SamplesPerSeekTableEntry { get; set; }

        /// <summary>The offset that the actual audio data starts at.</summary>
        public int AudioDataOffset { get; set; }
        public Reference AudioReference { get; set; }
        public Reference RegionReference { get; set; }

        public int RegionInfoSize { get; set; }
        public int LoopStartUnaligned { get; set; }
        public int LoopEndUnaligned { get; set; }
        public uint Checksum { get; set; }

        public static StreamInfo ReadBfstm(BinaryReader reader, NwVersion version)
        {
            var info = new StreamInfo
            {
                Codec = (NwCodec)reader.ReadByte(),
                Looping = reader.ReadBoolean(),
                ChannelCount = reader.ReadByte(),
                RegionCount = reader.ReadByte(),
                SampleRate = reader.ReadInt32(),
                LoopStart = reader.ReadInt32(),
                SampleCount = reader.ReadInt32(),
                InterleaveCount = reader.ReadInt32(),
                InterleaveSize = reader.ReadInt32(),
                SamplesPerInterleave = reader.ReadInt32(),
                LastBlockSizeWithoutPadding = reader.ReadInt32(),
                LastBlockSamples = reader.ReadInt32(),
                LastBlockSize = reader.ReadInt32(),
                BytesPerSeekTableEntry = reader.ReadInt32(),
                SamplesPerSeekTableEntry = reader.ReadInt32(),
                AudioReference = new Reference(reader)
            };

            if (Common.IncludeRegionInfo(version))
            {
                info.RegionInfoSize = reader.ReadInt16();
                reader.BaseStream.Position += 2;
                info.RegionReference = new Reference(reader);
            }

            if (Common.IncludeUnalignedLoop(version))
            {
                info.LoopStartUnaligned = reader.ReadInt32();
                info.LoopEndUnaligned = reader.ReadInt32();
            }

            if (Common.IncludeChecksum(version))
            {
                info.Checksum = reader.ReadUInt32();
            }

            return info;
        }

        public static StreamInfo ReadBfwav(BinaryReader reader, NwVersion version)
        {
            var info = new StreamInfo();
            info.Codec = (NwCodec)reader.ReadByte();
            info.Looping = reader.ReadBoolean();
            reader.BaseStream.Position += 2;
            info.SampleRate = reader.ReadInt32();
            info.LoopStart = reader.ReadInt32();
            info.SampleCount = reader.ReadInt32();

            if (Common.IncludeUnalignedLoopWave(version))
            {
                info.LoopStartUnaligned = reader.ReadInt32();
            }
            else
            {
                reader.BaseStream.Position += 4;
            }

            //Peek at the number of entries in the reference table
            info.ChannelCount = reader.ReadInt32();
            reader.BaseStream.Position -= 4;
            return info;
        }

        public static StreamInfo ReadBrstm(BinaryReader reader)
        {
            var info = new StreamInfo();
            info.Codec = (NwCodec)reader.ReadByte();
            info.Looping = reader.ReadBoolean();
            info.ChannelCount = reader.ReadByte();
            reader.BaseStream.Position += 1;

            info.SampleRate = reader.ReadUInt16();
            reader.BaseStream.Position += 2;

            info.LoopStart = reader.ReadInt32();
            info.SampleCount = reader.ReadInt32();
            info.AudioDataOffset = reader.ReadInt32();
            info.InterleaveCount = reader.ReadInt32();
            info.InterleaveSize = reader.ReadInt32();
            info.SamplesPerInterleave = reader.ReadInt32();
            info.LastBlockSizeWithoutPadding = reader.ReadInt32();
            info.LastBlockSamples = reader.ReadInt32();
            info.LastBlockSize = reader.ReadInt32();
            info.SamplesPerSeekTableEntry = reader.ReadInt32();

            return info;
        }
    }
}