using System;
using System.IO;
using System.Linq;
using VGAudio.Codecs.GcAdpcm;
using VGAudio.Formats;
using VGAudio.Formats.GcAdpcm;
using VGAudio.Utilities;
using static VGAudio.Codecs.GcAdpcm.GcAdpcmMath;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Containers.Hps
{
    public class HpsWriter : AudioWriter<HpsWriter, HpsConfiguration>
    {
        private GcAdpcmFormat Adpcm { get; set; }
        private BlockInfo[] BlockMap { get; set; }

        protected override int FileSize => HeaderSize + BlockMap.Sum(x => x.TotalSize);
        private readonly int MaxBlockSize = 0x10000;
        private int MaxBlockSizeActual { get; set; }
        private int HeaderSize => GetNextMultiple(Math.Max(0x80, 0x10 + 0x38 * ChannelCount), 0x20);

        private int ChannelCount => Adpcm.ChannelCount;
        private int SampleCount => Adpcm.SampleCount;

        protected override void SetupWriter(AudioData audio)
        {
            Adpcm = audio.GetFormat<GcAdpcmFormat>(new GcAdpcmParameters { Progress = Configuration.Progress });
            int channelSize = GetNextMultiple(MaxBlockSize / ChannelCount, 0x20);
            MaxBlockSizeActual = channelSize * ChannelCount;
            int alignment = ByteCountToSampleCount(channelSize);
            if (!LoopPointsAreAligned(Adpcm.LoopStart, alignment))
            {
                Adpcm = Adpcm.GetCloneBuilder().WithAlignment(alignment).Build();
            }

            Parallel.For(0, ChannelCount, i =>
            {
                GcAdpcmChannelBuilder builder = Adpcm.Channels[i].GetCloneBuilder();

                builder.LoopAlignmentMultiple = alignment;
                builder.EnsureLoopContextIsSelfCalculated = true;
                Adpcm.Channels[i] = builder.Build();
            });

            BlockMap = CreateBlockMap(Adpcm.SampleCount, Adpcm.Looping, Adpcm.LoopStart, Adpcm.ChannelCount, MaxBlockSizeActual);
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
            writer.Write(SampleToNibble(0));
            writer.Write(SampleToNibble(SampleCount - 1));
            writer.Write(SampleToNibble(0));
            writer.Write(channel.Coefs.ToByteArray(Endianness.BigEndian));
            writer.Write(channel.Gain);
            channel.StartContext.Write(writer);
        }

        private void WriteBody(BinaryWriter writer)
        {
            foreach (BlockInfo block in BlockMap)
            {
                WriteBlock(writer, block);
            }
        }

        private void WriteBlock(BinaryWriter writer, BlockInfo block)
        {
            writer.Write(block.WrittenSize);
            writer.Write(block.EndNibble);
            writer.Write(block.NextOffset);

            for (int i = 0; i < ChannelCount; i++)
            {
                writer.Write((short)Adpcm.Channels[i].GetPredScale(block.StartSample));
                writer.Write(Adpcm.Channels[i].GetHist1(block.StartSample));
                writer.Write(Adpcm.Channels[i].GetHist2(block.StartSample));
                writer.Write((short)0);
            }

            writer.BaseStream.Position = GetNextMultiple((int)writer.BaseStream.Position, 0x20);

            for (int i = 0; i < ChannelCount; i++)
            {
                writer.Write(Adpcm.Channels[i].GetAdpcmAudio(), block.ByteInIndex, block.ChannelSize);
                writer.BaseStream.Position = GetNextMultiple((int)writer.BaseStream.Position, 0x20);
            }
        }

        private BlockInfo[] CreateBlockMap(int sampleCount, bool loops, int loopStart, int channelCount, int maxBlockSizeBytes)
        {
            int nibbleCount = SampleCountToNibbleCount(sampleCount);
            int maxChannelBlockSize = maxBlockSizeBytes / channelCount * 2;
            int blockCount = nibbleCount.DivideByRoundUp(maxChannelBlockSize);
            int loopBlock = loops ? SampleToNibble(loopStart) / maxChannelBlockSize : blockCount - 1;

            if (!LoopPointsAreAligned(loopStart, NibbleCountToSampleCount(maxChannelBlockSize)))
            {
                return new BlockInfo[0];
            }

            var blocks = new BlockInfo[blockCount];

            int currentNibble = 0;
            int currentBlock = 0;

            while (currentBlock < loopBlock)
            {
                blocks[currentBlock++] = new BlockInfo(currentNibble, maxChannelBlockSize, channelCount);
                currentNibble += maxChannelBlockSize;
            }

            while (currentNibble < nibbleCount)
            {
                int blocksRemaining = blockCount - currentBlock;
                int nibblesRemaining = nibbleCount - currentNibble;
                int channelBlockSize = Math.Min(nibblesRemaining, GetNextMultiple(nibblesRemaining.DivideByRoundUp(blocksRemaining), 0x40));

                blocks[currentBlock++] = new BlockInfo(currentNibble, channelBlockSize, channelCount);
                currentNibble += channelBlockSize;
            }

            blocks[0].Offset = HeaderSize;
            for (int i = 1; i < blockCount; i++)
            {
                blocks[i].Offset = blocks[i - 1].Offset + blocks[i - 1].TotalSize;
            }

            for (int i = 0; i < blockCount - 1; i++)
            {
                blocks[i].NextOffset = blocks[i + 1].Offset;
            }

            blocks[blockCount - 1].NextOffset = loops ? blocks[loopBlock].Offset : -1;
            return blocks;
        }

        private class BlockInfo
        {
            public BlockInfo(int currentNibble, int channelSizeNibbles, int channelCount)
            {
                // Add 2 to account for the predictor and scale nibbles that don't count as samples;
                StartSample = NibbleToSample(currentNibble + 2);
                ByteInIndex = currentNibble / 2;
                EndNibble = channelSizeNibbles - 1;
                ChannelSize = channelSizeNibbles.DivideBy2RoundUp();
                WrittenSize = GetNextMultiple(ChannelSize, 0x20) * channelCount;
                TotalSize = GetNextMultiple(4 + 8 * channelCount, 0x20) + WrittenSize;
            }

            public int Offset;
            public int NextOffset;
            public int StartSample;
            public int ByteInIndex;
            public int TotalSize;
            public int WrittenSize;
            public int ChannelSize;
            public int EndNibble;
        }
    }
}
