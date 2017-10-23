using System.IO;
using System.Linq;
using VGAudio.Formats;
using VGAudio.Formats.GcAdpcm;
using VGAudio.Utilities;
using static VGAudio.Codecs.GcAdpcm.GcAdpcmMath;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Containers.Dsp
{
    public class DspReader : AudioReader<DspReader, DspStructure, DspConfiguration>
    {
        private static int HeaderSize => 0x60;

        protected override DspStructure ReadFile(Stream stream, bool readAudioData = true)
        {
            using (BinaryReader reader = GetBinaryReader(stream, Endianness.BigEndian))
            {
                var structure = new DspStructure();

                ReadHeader(reader, structure);

                if (readAudioData)
                {
                    reader.BaseStream.Position = HeaderSize * structure.ChannelCount;
                    ReadData(reader, structure);
                }

                return structure;
            }
        }

        protected override IAudioFormat ToAudioStream(DspStructure structure)
        {
            var channels = new GcAdpcmChannel[structure.ChannelCount];

            for (int c = 0; c < structure.ChannelCount; c++)
            {
                var channelBuilder = new GcAdpcmChannelBuilder(structure.AudioData[c], structure.Channels[c].Coefs, structure.SampleCount)
                {
                    Gain = structure.Channels[c].Gain,
                    StartContext = structure.Channels[c].Start
                };

                channelBuilder.WithLoop(structure.Looping, structure.LoopStart, structure.LoopEnd)
                    .WithLoopContext(structure.LoopStart, structure.Channels[c].Loop.PredScale,
                        structure.Channels[c].Loop.Hist1, structure.Channels[c].Loop.Hist2);

                channels[c] = channelBuilder.Build();
            }

            return new GcAdpcmFormatBuilder(channels, structure.SampleRate)
                .WithLoop(structure.Looping, structure.LoopStart, structure.LoopEnd)
                .Build();
        }

        private static void ReadHeader(BinaryReader reader, DspStructure structure)
        {
            structure.SampleCount = reader.ReadInt32();
            structure.NibbleCount = reader.ReadInt32();
            structure.SampleRate = reader.ReadInt32();
            structure.Looping = reader.ReadInt16() == 1;
            structure.Format = reader.ReadInt16();
            structure.StartAddress = reader.ReadInt32();
            structure.EndAddress = reader.ReadInt32();
            structure.CurrentAddress = reader.ReadInt32();

            reader.BaseStream.Position = 0x4a;
            structure.ChannelCount = reader.ReadInt16();
            structure.FramesPerInterleave = reader.ReadInt16();
            structure.ChannelCount = structure.ChannelCount == 0 ? 1 : structure.ChannelCount;

            for (int i = 0; i < structure.ChannelCount; i++)
            {
                reader.BaseStream.Position = HeaderSize * i + 0x1c;
                var channel = new GcAdpcmChannelInfo
                {
                    Coefs = Enumerable.Range(0, 16).Select(x => reader.ReadInt16()).ToArray(),
                    Gain = reader.ReadInt16(),
                    Start = new GcAdpcmContext(reader),
                    Loop = new GcAdpcmContext(reader)
                };

                structure.Channels.Add(channel);
            }

            if (reader.BaseStream.Length < HeaderSize + SampleCountToByteCount(structure.SampleCount))
            {
                throw new InvalidDataException($"File doesn't contain enough data for {structure.SampleCount} samples");
            }

            if (SampleCountToNibbleCount(structure.SampleCount) != structure.NibbleCount)
            {
                throw new InvalidDataException("Sample count and nibble count do not match");
            }

            if (structure.Format != 0)
            {
                throw new InvalidDataException($"File does not contain ADPCM audio. Specified format is {structure.Format}");
            }
        }

        private static void ReadData(BinaryReader reader, DspStructure structure)
        {
            if (structure.ChannelCount == 1)
            {
                structure.AudioData = new[] { reader.ReadBytes(SampleCountToByteCount(structure.SampleCount)) };
            }
            else
            {
                int dataLength = GetNextMultiple(SampleCountToByteCount(structure.SampleCount), 8) * structure.ChannelCount;
                int interleaveSize = structure.FramesPerInterleave * BytesPerFrame;
                structure.AudioData = reader.BaseStream.DeInterleave(dataLength, interleaveSize, structure.ChannelCount, SampleCountToByteCount(structure.SampleCount));
            }
        }
    }
}
