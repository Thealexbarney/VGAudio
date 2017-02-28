using System.IO;
using System.Text;
using DspAdpcm.Containers.Idsp;
using DspAdpcm.Formats;
using DspAdpcm.Formats.GcAdpcm;
using DspAdpcm.Utilities;
using static DspAdpcm.Formats.GcAdpcm.GcAdpcmHelpers;
using static DspAdpcm.Utilities.Helpers;

#if NET20
using DspAdpcm.Compatibility.LinqBridge;
#else
using System.Linq;
#endif

namespace DspAdpcm.Containers
{
    public class IdspReader : AudioReader<IdspReader, IdspStructure, IdspConfiguration>
    {
        protected override IdspStructure ReadFile(Stream stream, bool readAudioData = true)
        {
            using (BinaryReader reader = GetBinaryReader(stream, Endianness.BigEndian))
            {
                if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "IDSP")
                {
                    throw new InvalidDataException("File has no IDSP header");
                }

                var structure = new IdspStructure();

                ReadIdspHeader(reader, structure);
                if (readAudioData)
                {
                    ReadIdspData(reader, structure);
                }

                return structure;
            }
        }

        protected override IAudioFormat ToAudioStream(IdspStructure structure)
        {
            var channels = new GcAdpcmChannel[structure.ChannelCount];

            for (int c = 0; c < structure.ChannelCount; c++)
            {
                var channel = new GcAdpcmChannel(structure.SampleCount, structure.AudioData[c])
                {
                    Coefs = structure.Channels[c].Coefs,
                    Gain = structure.Channels[c].Gain,
                    Hist1 = structure.Channels[c].Hist1,
                    Hist2 = structure.Channels[c].Hist2
                };
                channel.SetLoopContext(structure.LoopStart, structure.Channels[c].LoopPredScale,
                    structure.Channels[c].LoopHist1, structure.Channels[c].LoopHist2);

                channels[c] = channel;
            }

            var adpcm = new GcAdpcmFormat(structure.SampleCount, structure.SampleRate, channels);
            adpcm.SetLoop(structure.Looping, structure.LoopStart, structure.LoopEnd);

            return adpcm;
        }

        protected override IdspConfiguration GetConfiguration(IdspStructure structure)
        {
            return new IdspConfiguration
            {
                BytesPerInterleave = structure.InterleaveSize
            };
        }

        private static void ReadIdspHeader(BinaryReader reader, IdspStructure structure)
        {
            reader.BaseStream.Position += 4;
            structure.ChannelCount = reader.ReadInt32();
            structure.SampleRate = reader.ReadInt32();
            structure.SampleCount = reader.ReadInt32();
            structure.LoopStart = reader.ReadInt32();
            structure.LoopEnd = reader.ReadInt32();
            structure.InterleaveSize = reader.ReadInt32();
            structure.HeaderSize = reader.ReadInt32();
            structure.ChannelInfoSize = reader.ReadInt32();
            structure.AudioDataOffset = reader.ReadInt32();
            structure.AudioDataLength = reader.ReadInt32();

            for (int i = 0; i < structure.ChannelCount; i++)
            {
                reader.BaseStream.Position = structure.HeaderSize + i * structure.ChannelInfoSize;
                var channel = new IdspChannelInfo();
                channel.SampleCount = reader.ReadInt32();
                channel.NibbleCount = reader.ReadInt32();
                channel.SampleRate = reader.ReadInt32();
                channel.Looping = reader.ReadInt16() == 1;
                reader.BaseStream.Position += 2;
                channel.StartAddress = reader.ReadInt32();
                channel.EndAddress = reader.ReadInt32();
                channel.CurrentAddress = reader.ReadInt32();
                channel.Coefs = Enumerable.Range(0, 16).Select(x => reader.ReadInt16()).ToArray();
                channel.Gain = reader.ReadInt16();
                channel.PredScale = reader.ReadInt16();
                channel.Hist1 = reader.ReadInt16();
                channel.Hist2 = reader.ReadInt16();
                channel.LoopPredScale = reader.ReadInt16();
                channel.LoopHist1 = reader.ReadInt16();
                channel.LoopHist2 = reader.ReadInt16();

                structure.Channels.Add(channel);
            }

            //There isn't a loop flag for the file in general, so we set it
            //if any of the single channels loop.
            structure.Looping = structure.Channels.Any(x => x.Looping);
        }

        private static void ReadIdspData(BinaryReader reader, IdspStructure structure)
        {
            reader.BaseStream.Position = structure.AudioDataOffset;
            int interleave = structure.InterleaveSize == 0 ? structure.AudioDataLength : structure.InterleaveSize;
            //If the file isn't interleaved, there is no padding/alignment at the break between channels.
            structure.AudioData = reader.BaseStream.DeInterleave(structure.ChannelCount * structure.AudioDataLength, interleave,
                structure.ChannelCount, SampleCountToByteCount(structure.SampleCount));
        }
    }
}