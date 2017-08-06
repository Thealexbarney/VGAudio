using System.IO;
using System.Linq;
using VGAudio.Containers.NintendoWare.Structures;
using VGAudio.Formats;
using VGAudio.Utilities;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Containers.NintendoWare
{
    public class BrwavReader : AudioReader<BrwavReader, BrwavStructure, BxstmConfiguration>
    {
        protected override BrwavStructure ReadFile(Stream stream, bool readAudioData = true)
        {
            using (BinaryReader reader = GetBinaryReader(stream, Endianness.BigEndian))
            {
                if (reader.ReadUTF8(4) != "RWAV")
                {
                    throw new InvalidDataException("File has no RWAV header");
                }

                var structure = new BrwavStructure();

                ReadRwavHeader(reader, structure);
                ReadInfoBlock(reader, structure);
                ReadDataBlock(reader, structure, readAudioData);

                return structure;
            }
        }

        protected override IAudioFormat ToAudioStream(BrwavStructure structure) => Common.ToAudioStream(structure);

        private static void ReadRwavHeader(BinaryReader reader, BrwavStructure structure)
        {
            reader.Expect((ushort)0xfeff);
            structure.Version = new NwVersion(reader.ReadByte(), reader.ReadByte());
            structure.FileSize = reader.ReadInt32();

            if (reader.BaseStream.Length < structure.FileSize)
            {
                throw new InvalidDataException("Actual file length is less than stated length");
            }

            structure.HeaderSize = reader.ReadInt16();
            structure.BlockCount = reader.ReadInt16();

            structure.BrstmHeader = BrstmHeader.ReadBrwav(reader);
        }

        private static void ReadInfoBlock(BinaryReader reader, BrwavStructure structure)
        {
            reader.BaseStream.Position = structure.BrstmHeader.HeadBlockOffset;

            if (reader.ReadUTF8(4) != "INFO")
            {
                throw new InvalidDataException("Unknown or invalid INFO block");
            }

            if (reader.ReadInt32() != structure.BrstmHeader.HeadBlockSize)
            {
                throw new InvalidDataException("HEAD block size in RWAV header doesn't match size in HEAD header");
            }

            RwavWaveInfo info = RwavWaveInfo.ReadBrwav(reader);
            structure.WaveInfo = info;

            structure.StreamInfo = info;
            structure.ChannelInfo = new ChannelInfo();
            structure.ChannelInfo.Channels.AddRange(info.Channels.Select(x => x.AdpcmInfo));
        }

        private static void ReadDataBlock(BinaryReader reader, BrwavStructure structure, bool readAudioData)
        {
            reader.BaseStream.Position = structure.BrstmHeader.DataBlockOffset;
            RwavWaveInfo info = structure.WaveInfo;

            if (reader.ReadUTF8(4) != "DATA")
            {
                throw new InvalidDataException("Unknown or invalid DATA block");
            }

            if (reader.ReadInt32() != structure.BrstmHeader.DataBlockSize)
            {
                throw new InvalidDataException("DATA block size in main header doesn't match size in DATA header");
            }

            if (!readAudioData) return;

            int audioDataLength = Common.SamplesToBytes(info.SampleCount, info.Codec);
            structure.AudioData = CreateJaggedArray<byte[][]>(info.ChannelCount, audioDataLength);
            int baseOffset = (int)reader.BaseStream.Position;

            for (int i = 0; i < info.ChannelCount; i++)
            {
                reader.BaseStream.Position = baseOffset + info.Channels[i].AudioDataOffset;
                structure.AudioData[i] = reader.ReadBytes(audioDataLength);
            }
        }
    }
}
