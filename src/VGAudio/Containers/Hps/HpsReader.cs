using System;
using System.IO;
using System.Linq;
using VGAudio.Codecs.GcAdpcm;
using VGAudio.Formats;
using VGAudio.Formats.GcAdpcm;
using VGAudio.Utilities;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Containers.Hps
{
    public class HpsReader : AudioReader<HpsReader, HpsStructure, HpsConfiguration>
    {
        protected override HpsStructure ReadFile(Stream stream, bool readAudioData = true)
        {
            using (BinaryReader reader = GetBinaryReader(stream, Endianness.BigEndian))
            {
                if (reader.ReadUTF8(8) != " HALPST\0")
                {
                    throw new InvalidDataException("File has no HALPST header");
                }

                var structure = new HpsStructure();

                ReadHeader(reader, structure);
                stream.Position = GetNextMultiple(Math.Max(0x80, (int)stream.Position), 0x20);
                ReadData(reader, structure);
                VerifyData(structure);

                return structure;
            }
        }

        protected override IAudioFormat ToAudioStream(HpsStructure structure)
        {
            var channels = new GcAdpcmChannel[structure.ChannelCount];

            for (int c = 0; c < structure.ChannelCount; c++)
            {
                int audioLength = structure.Blocks.Sum(x => x.Channels[c].AudioData.Length);
                var audio = new byte[audioLength];
                int currentPosition = 0;

                foreach (HpsBlock block in structure.Blocks)
                {
                    byte[] source = block.Channels[c].AudioData;
                    Array.Copy(source, 0, audio, currentPosition, source.Length);
                    currentPosition += source.Length;
                }

                var channelBuilder = new GcAdpcmChannelBuilder(audio, structure.Channels[c].Coefs, structure.Channels[c].SampleCount)
                {
                    Gain = structure.Channels[c].Gain,
                    StartContext = structure.Channels[c].Start
                };

                channelBuilder.WithLoop(structure.Looping, structure.LoopStart, structure.SampleCount)
                    .WithLoopContext(structure.LoopStart, structure.Channels[c].Loop?.PredScale ?? 0,
                        structure.Channels[c].Loop?.Hist1 ?? 0, structure.Channels[c].Loop?.Hist2 ?? 0);

                channels[c] = channelBuilder.Build();
            }

            return new GcAdpcmFormatBuilder(channels, structure.SampleRate)
                .WithLoop(structure.Looping, structure.LoopStart, structure.SampleCount)
                .Build();
        }

        private static void ReadHeader(BinaryReader reader, HpsStructure structure)
        {
            structure.SampleRate = reader.ReadInt32();
            structure.ChannelCount = reader.ReadInt32();

            for (int i = 0; i < structure.ChannelCount; i++)
            {
                var channel = new HpsChannelInfo();
                channel.MaxBlockSize = reader.ReadInt32();
                reader.ReadInt32();
                channel.EndAddress = reader.ReadInt32();
                reader.ReadInt32();
                channel.Coefs = Enumerable.Range(0, 16).Select(x => reader.ReadInt16()).ToArray();
                channel.Gain = reader.ReadInt16();
                channel.Start = new GcAdpcmContext(reader);

                structure.Channels.Add(channel);
            }
        }

        private static void ReadData(BinaryReader reader, HpsStructure structure)
        {
            int currentBlock = 0;
            int nextBlock = (int)reader.BaseStream.Position;

            while (nextBlock > currentBlock)
            {
                reader.BaseStream.Position = nextBlock;
                currentBlock = nextBlock;

                var block = new HpsBlock
                {
                    Offset = currentBlock,
                    Size = reader.ReadInt32(),
                    FinalNibble = reader.ReadInt32(),
                    NextOffset = reader.ReadInt32()
                };

                for (int i = 0; i < structure.ChannelCount; i++)
                {
                    block.Channels.Add(new HpsBlockChannel
                    {
                        Context = new GcAdpcmContext(reader)
                    });
                    reader.BaseStream.Position += 2;
                }

                MoveToNextMultiple(reader, 0x20);
                int audioStart = (int)reader.BaseStream.Position;

                for (int i = 0; i < structure.ChannelCount; i++)
                {
                    reader.BaseStream.Position = audioStart + block.Size / structure.ChannelCount * i;
                    block.Channels[i].AudioData = reader.ReadBytes(block.AudioSizeBytes);
                }

                structure.Blocks.Add(block);
                nextBlock = block.NextOffset;
            }
        }

        private static void VerifyData(HpsStructure structure)
        {
            structure.SampleCount = structure.Channels.First().SampleCount;
            if (structure.Channels.Any(x => x.SampleCount != structure.SampleCount))
            {
                throw new InvalidDataException("Channels have differing sample counts");
            }

            int startOffset = structure.Blocks.Last().NextOffset;
            if (startOffset == -1) return;

            int currentNibble = 0;

            //Find the loop start block
            foreach (HpsBlock block in structure.Blocks)
            {
                if (block.Offset == startOffset)
                {
                    structure.Looping = true;
                    structure.LoopStart = GcAdpcmMath.NibbleCountToSampleCount(currentNibble);

                    for (int i = 0; i < structure.Channels.Count; i++)
                    {
                        structure.Channels[i].Loop = block.Channels[i].Context;
                    }
                }
                currentNibble += block.FinalNibble + 1;
            }
        }

        private static void MoveToNextMultiple(BinaryReader reader, int multiple)
        {
            reader.BaseStream.Position = GetNextMultiple((int)reader.BaseStream.Position, multiple);
        }
    }
}
