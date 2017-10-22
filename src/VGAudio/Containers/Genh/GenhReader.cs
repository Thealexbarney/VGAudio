using System.IO;
using System.Linq;
using VGAudio.Formats;
using VGAudio.Formats.GcAdpcm;
using VGAudio.Utilities;
using static VGAudio.Codecs.GcAdpcm.GcAdpcmMath;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Containers.Genh
{
    public class GenhReader : AudioReader<GenhReader, GenhStructure, GenhConfiguration>
    {
        protected override GenhStructure ReadFile(Stream stream, bool readAudioData = true)
        {
            using (BinaryReader reader = GetBinaryReader(stream, Endianness.LittleEndian))
            {
                var structure = new GenhStructure();

                ReadHeader(reader, structure);

                if (readAudioData)
                {
                    reader.BaseStream.Position = structure.AudioDataOffset;
                    ReadData(reader, structure);
                }

                return structure;
            }
        }

        protected override IAudioFormat ToAudioStream(GenhStructure structure)
        {
            var channels = new GcAdpcmChannel[structure.ChannelCount];

            for (int c = 0; c < structure.ChannelCount; c++)
            {
                GcAdpcmChannel channel = 
                    new GcAdpcmChannelBuilder(structure.AudioData[c], structure.Channels[c].Coefs, structure.SampleCount)
                        .WithLoop(structure.Looping, structure.LoopStart, structure.LoopEnd)
                        .Build();

                channels[c] = channel;
            }

            return new GcAdpcmFormatBuilder(channels, structure.SampleRate) 
                .WithLoop(structure.Looping, structure.LoopStart, structure.LoopEnd)
                .Build();
        }

        private static void ReadHeader(BinaryReader reader, GenhStructure structure)
        {
            if (reader.ReadUTF8(4) != "GENH")
            {
                throw new InvalidDataException("File has no GENH header");
            }

            structure.ChannelCount = reader.ReadInt32();
            structure.Interleave = reader.ReadInt32();
            structure.SampleRate = reader.ReadInt32();
            structure.LoopStart = reader.ReadInt32();
            structure.LoopEnd = reader.ReadInt32();
            structure.Codec = reader.ReadInt32();
            structure.AudioDataOffset = reader.ReadInt32();
            structure.HeaderSize = reader.ReadInt32();
            structure.Coefs[0] = reader.ReadInt32();
            structure.Coefs[1] = reader.ReadInt32();
            structure.InterleaveType = reader.ReadInt32();
            structure.CoefType = (GenhCoefType)reader.ReadInt32();
            structure.CoefsSplit[0] = reader.ReadInt32();
            structure.CoefsSplit[1] = reader.ReadInt32();

            if (structure.ChannelCount < 1)
            {
                throw new InvalidDataException("File must have at least one channel.");
            }

            if (structure.ChannelCount > 2)
            {
                throw new InvalidDataException("GENH does not support more than 2 channels with NGC DSP files.");
            }

            if (structure.HeaderSize > structure.AudioDataOffset)
            {
                throw new InvalidDataException("Audio data must come after the GENH header.");
            }

            for (int i = 0; i < structure.ChannelCount; i++)
            {
                ReadCoefs(reader, structure, i);
            }
        }

        private static void ReadCoefs(BinaryReader reader, GenhStructure structure, int channelNum)
        {
            using (BinaryReader coefReader = GetBinaryReader(reader.BaseStream, structure.CoefType.Endianness()))
            {
                coefReader.BaseStream.Position = structure.Coefs[channelNum];
                var channel = new GcAdpcmChannelInfo();

                if (structure.CoefType.HasFlag(GenhCoefType.Split))
                {
                    channel.Coefs = new short[16];
                    for (int c = 0; c < 8; c++)
                    {
                        channel.Coefs[c * 2] = coefReader.ReadInt16();
                    }

                    coefReader.BaseStream.Position = structure.CoefsSplit[channelNum];
                    for (int c = 0; c < 8; c++)
                    {
                        channel.Coefs[c * 2 + 1] = coefReader.ReadInt16();
                    }
                }
                else
                {
                    channel.Coefs = Enumerable.Range(0, 16).Select(x => coefReader.ReadInt16()).ToArray();
                }

                structure.Channels.Add(channel);
            }
        }

        private static void ReadData(BinaryReader reader, GenhStructure structure)
        {
            int dataLength = SampleCountToByteCount(structure.SampleCount) * structure.ChannelCount;
            structure.AudioData = reader.BaseStream.DeInterleave(dataLength, structure.Interleave, structure.ChannelCount);
        }
    }
}