using System;
using System.IO;
using System.Linq;
using VGAudio.Containers.Hps;
using VGAudio.Formats;
using VGAudio.Formats.GcAdpcm;
using VGAudio.Utilities;
using static VGAudio.Formats.GcAdpcm.GcAdpcmHelpers;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Containers
{
    public class HpsWriter : AudioWriter<HpsWriter, HpsConfiguration>
    {
        private GcAdpcmFormat Adpcm { get; set; }
        private BlockInfo[] BlockMap { get; set; }

        protected override int FileSize => HeaderSize + BlockMap.Sum(x => x.TotalSize);
        private readonly int MaxBlockSize = 0x10000;
        private int HeaderSize => GetNextMultiple(Math.Max(0x80, 0x10 + 0x38 * ChannelCount), 0x20);

        private int ChannelCount => Adpcm.ChannelCount;
        private int SampleCount => Adpcm.SampleCount;

        protected override void SetupWriter(AudioData audio)
        {
            Adpcm = audio.GetFormat<GcAdpcmFormat>();
            BlockMap = CreateBlockMap(Adpcm.SampleCount, Adpcm.LoopStart, Adpcm.ChannelCount, MaxBlockSize);
        }

        protected override void WriteStream(Stream stream)
        {
            using (BinaryWriter writer = GetBinaryWriter(stream, Endianness.BigEndian))
            {
                stream.Position = 0;
                WriteHpsHeader(writer);
                stream.Position = HeaderSize;
                WriteBody(writer);
            }
        }

        private void WriteHpsHeader(BinaryWriter writer)
        {
            writer.WriteUTF8(" HALPST\0");
            writer.Write(Adpcm.SampleRate);
            writer.Write(ChannelCount);

            foreach (GcAdpcmChannel channel in Adpcm.Channels)
            {
                WriteChannelInfo(writer, channel);
            }
        }

        private void WriteChannelInfo(BinaryWriter writer, GcAdpcmChannel channel)
        {
            writer.Write(MaxBlockSize);
            writer.Write(2);
            writer.Write(SampleCountToNibbleCount(SampleCount));
            writer.Write(2);
            writer.Write(channel.Coefs.ToByteArray(Endianness.BigEndian));
            writer.Write(channel.Gain);
            writer.Write(channel.PredScale);
            writer.Write(channel.Hist1);
            writer.Write(channel.Hist2);
        }

        private void WriteBody(BinaryWriter writer)
        {
            int baseOffset = (int)writer.BaseStream.Position;

            foreach (BlockInfo block in BlockMap)
            {
                WriteBlock(writer, block, baseOffset);
            }
        }

        private void WriteBlock(BinaryWriter writer, BlockInfo block, int baseOffset)
        {
            writer.Write(block.WrittenSize);
            writer.Write(block.ChannelSize * 2 - 1);
            writer.Write(baseOffset + block.NextOffset);

            for (int i = 0; i < ChannelCount; i++)
            {
                writer.Write((short)0);
                writer.Write((short)0);
                writer.Write((short)0);
                writer.Write((short)0);
            }

            writer.BaseStream.Position = GetNextMultiple((int)writer.BaseStream.Position, 0x20);

            for (int i = 0; i < ChannelCount; i++)
            {
                writer.Write(Adpcm.Channels[i].Adpcm, block.ByteInIndex, block.ChannelSize);
                writer.BaseStream.Position = GetNextMultiple((int)writer.BaseStream.Position, 0x20);
            }
        }

        private static BlockInfo[] CreateBlockMap(int sampleCount, int loopStart, int channelCount, int maxBlockSize)
        {
            int byteCount = SampleCountToByteCount(sampleCount);
            int maxChannelBlockSize = maxBlockSize / channelCount;
            int blockCount = byteCount.DivideByRoundUp(maxChannelBlockSize);
            int loopBlock = SampleCountToByteCount(loopStart) / maxChannelBlockSize;

            if (!LoopPointsAreAligned(loopStart, NibbleCountToSampleCount(maxChannelBlockSize * 2)))
            {
                return new BlockInfo[0];
            }

            var blocks = new BlockInfo[blockCount];

            int currentByte = 0;
            int currentBlock = 0;

            while (currentBlock < loopBlock)
            {
                blocks[currentBlock++] = new BlockInfo(currentByte, maxChannelBlockSize, channelCount);
                currentByte += maxChannelBlockSize;
            }

            while (currentByte < byteCount)
            {
                int blocksRemaining = blockCount - currentBlock;
                int bytesRemaining = byteCount - currentByte;
                int channelBlockSize = Math.Min(bytesRemaining, GetNextMultiple(bytesRemaining / blocksRemaining, 0x20));

                blocks[currentBlock++] = new BlockInfo(currentByte, channelBlockSize, channelCount);
                currentByte += channelBlockSize;
            }

            for (int i = 1; i < blockCount; i++)
            {
                blocks[i].Offset = blocks[i - 1].Offset + blocks[i - 1].TotalSize;
            }

            for (int i = 0; i < blockCount - 1; i++)
            {
                blocks[i].NextOffset = blocks[i + 1].Offset;
            }

            blocks[blockCount - 1].NextOffset = blocks[loopBlock].Offset;

            return blocks;
        }

        private class BlockInfo
        {
            public BlockInfo(int currentByte, int channelSize, int channelCount)
            {
                ByteInIndex = currentByte;
                ChannelSize = channelSize;
                WrittenSize = GetNextMultiple(channelSize, 0x20) * channelCount;
                TotalSize = GetNextMultiple(4 + 8 * channelCount, 0x20) + WrittenSize;
            }

            public int Offset;
            public int NextOffset;
            public int ByteInIndex;
            public int TotalSize;
            public int WrittenSize;
            public int ChannelSize;
        }
    }
}
