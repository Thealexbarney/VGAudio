using System.IO;
using System.Linq;
using VGAudio.Formats;
using VGAudio.Formats.GcAdpcm;
using VGAudio.Utilities;
using static VGAudio.Codecs.GcAdpcm.GcAdpcmMath;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Containers.McAdpcm
{
    public class McAdpcmReader : AudioReader<McAdpcmReader, McAdpcmStructure, McAdpcmConfiguration>
    {
        private static int DspHeaderCoefOffset = 0x1C;
        private static int DspHeaderSize => 0x60;

        protected override McAdpcmStructure ReadFile(Stream stream, bool readAudioData = true)
        {
            var structure = new McAdpcmStructure();

            ReadHeader(GetBinaryReader(stream, Endianness.LittleEndian), structure);

            if (readAudioData) {
                using (BinaryReader reader = GetBinaryReader(stream, Endianness.BigEndian))
                {
                    reader.BaseStream.Position = structure.McadpcmHeaderSize;
                    ReadData(reader, structure);
                }
            }

            return structure;
        }

        protected override IAudioFormat ToAudioStream(McAdpcmStructure structure)
        {
            var channels = new GcAdpcmChannel[structure.ChannelCount];

            for (int c = 0; c < structure.ChannelCount; c++)
            {
                var channelBuilder = new GcAdpcmChannelBuilder(structure.AudioData[c], structure.Channels[c].Coefs, structure.SampleCount)
                {
                    Gain = structure.Channels[c].Gain,
                    StartContext = structure.Channels[c].Start
                };

                channelBuilder
                    .WithLoop(structure.Looping, structure.LoopStart, structure.LoopEnd)
                    .WithLoopContext(structure.LoopStart, structure.Channels[c].Loop.PredScale,
                        structure.Channels[c].Loop.Hist1, structure.Channels[c].Loop.Hist2);

                channels[c] = channelBuilder.Build();
            }

            return new GcAdpcmFormatBuilder(channels, structure.SampleRate)
                .WithLoop(structure.Looping, structure.LoopStart, structure.LoopEnd)
                .Build();
        }

        private static void ReadHeader(BinaryReader reader, McAdpcmStructure structure)
        {
            structure.ChannelCount = reader.ReadInt32();

            if (structure.ChannelCount > 2)
            {
                throw new InvalidDataException($"McAdpcm cannot have more than 2 channels.");
            }

            structure.McadpcmHeaderSize = reader.ReadInt32();
            structure.DspChannelDataSize = reader.ReadInt32(); // channel 0 data size

            // A Stereo MCADPCM has 2 additional 32 bit fields  we don't need for computation
            // - channel1 data offset from file start
            // - channel1 data size (never found a case where <> than Channel0 data size)
            reader.BaseStream.Position += (structure.ChannelCount - 1) * 0x02 * sizeof(int);

            structure.SampleCount = reader.ReadInt32();
            structure.NibbleCount = reader.ReadInt32();
            structure.SampleRate = reader.ReadInt32();
            structure.Looping = reader.ReadInt16() == 1;
            structure.Format = reader.ReadInt16();
            structure.StartAddress = reader.ReadInt32();
            structure.EndAddress = reader.ReadInt32();
            structure.CurrentAddress = reader.ReadInt32();

            structure.Channels.Add(new GcAdpcmChannelInfo
            {
                Coefs = Enumerable.Range(0, 16).Select(x => reader.ReadInt16()).ToArray(),
                Gain = reader.ReadInt16(),
                Start = new GcAdpcmContext(reader),
                Loop = new GcAdpcmContext(reader)
            });

            if (structure.ChannelCount == 2)
            {
                reader.BaseStream.Position = structure.McadpcmHeaderSize + structure.DspChannelDataSize + DspHeaderCoefOffset;

                structure.Channels.Add(new GcAdpcmChannelInfo
                {
                    Coefs = Enumerable.Range(0, 16).Select(x => reader.ReadInt16()).ToArray(),
                    Gain = reader.ReadInt16(),
                    Start = new GcAdpcmContext(reader),
                    Loop = new GcAdpcmContext(reader)
                });
            }

            if (reader.BaseStream.Length < structure.McadpcmHeaderSize + structure.DspChannelDataSize * structure.ChannelCount)
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

        private static void ReadData(BinaryReader reader, McAdpcmStructure structure)
        {
            structure.AudioData = new byte[structure.ChannelCount][];

            for (int i = 0; i < structure.ChannelCount; i++)
            {
                reader.BaseStream.Position = structure.McadpcmHeaderSize + DspHeaderSize + (structure.DspChannelDataSize) * i;
                structure.AudioData[i] = reader.ReadBytes(structure.DspChannelDataSize - DspHeaderSize);
            }
        }
    }
}