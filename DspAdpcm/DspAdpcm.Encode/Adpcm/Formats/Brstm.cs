using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DspAdpcm.Encode.Helpers;

namespace DspAdpcm.Encode.Adpcm.Formats
{
    public class Brstm
    {
        public AdpcmStream AudioStream { get; set; }

        private int NumSamples => AudioStream.NumSamples;
        private int NumChannels => AudioStream.Channels.Count;
        private byte Codec { get; } = 2; // 4-bit ADPCM
        private byte Looping => (byte)(AudioStream.Looping ? 1 : 0);
        private int AudioDataOffset => DataChunkOffset + 0x20;
        private int InterleaveSize => GetBytesForAdpcmSamples(SamplesPerInterleave);
        private int SamplesPerInterleave { get; set; } = 0x3800;
        private int InterleaveCount => (NumSamples / SamplesPerInterleave) + (LastBlockSamples == 0 ? 0 : 1);
        private int LastBlockSizeWithoutPadding => GetBytesForAdpcmSamples(LastBlockSamples);
        private int LastBlockSamples => NumSamples % SamplesPerInterleave;
        private int LastBlockSize => GetNextMultiple(LastBlockSizeWithoutPadding, 0x20);
        private int SamplesPerAdpcEntry { get; set; } = 0x3800;
        private int NumAdpcEntries => (NumSamples / SamplesPerAdpcEntry) - (LastBlockSamples == 0 ? 1 : 0);
        private int BytesPerAdpcEntry => 4; //Or is it bits per sample?

        private int RstmHeaderLength { get; set; } = 0x40;

        private int HeadChunkOffset => RstmHeaderLength;
        private int HeadChunkLength => GetNextMultiple(HeadChunkHeaderLength + HeadChunkTableLength +
            HeadChunk1Length + HeadChunk2Length + HeadChunk3Length, 0x20);
        private int HeadChunkHeaderLength = 8;
        private int HeadChunkTableLength => 8 * 3;
        private int HeadChunk1Length => 0x34;
        private int HeadChunk2Length { get; set; } = 0x10;
        private int HeadChunk3Length => 4 + (8 * NumChannels) + (ChannelInfoLength * NumChannels);
        private int ChannelInfoLength => 0x38;

        private int AdpcChunkOffset => RstmHeaderLength + HeadChunkLength;
        private int AdpcChunkLength => GetNextMultiple(0x10 + NumAdpcEntries * NumChannels * BytesPerAdpcEntry, 0x20);

        private int DataChunkOffset => RstmHeaderLength + HeadChunkLength + AdpcChunkLength;
        private int DataChunkLength => GetNextMultiple(0x20 + (InterleaveCount - (LastBlockSamples == 0 ? 0 : 1)) * InterleaveSize * NumChannels + LastBlockSize * NumChannels, 0x20);

        private int FileLength => RstmHeaderLength + HeadChunkLength + AdpcChunkLength + DataChunkLength;

        public Brstm(IAdpcmStream stream)
        {
            if (stream.Channels.Count < 1)
            {
                throw new InvalidDataException("Stream must have at least one channel ");
            }

            AudioStream = stream as AdpcmStream;
        }

        public Brstm(Stream stream)
        {
            ReadBrstmFile(stream);
        }

        public IEnumerable<byte> GetFile()
        {
            return Combine(GetRstmHeader(), GetHeadChunk(), GetAdpcChunk(), GetDataChunk());
        }

        private byte[] GetRstmHeader()
        {
            var header = new List<byte>();

            header.Add32("RSTM");
            header.Add16BE(0xfeff); //Endianness
            header.Add16BE(0x0100); //BRSTM format version
            header.Add32BE(FileLength);
            header.Add16BE(RstmHeaderLength);
            header.Add16BE(2); // NumEntries
            header.Add32BE(HeadChunkOffset);
            header.Add32BE(HeadChunkLength);
            header.Add32BE(AdpcChunkOffset);
            header.Add32BE(AdpcChunkLength);
            header.Add32BE(DataChunkOffset);
            header.Add32BE(DataChunkLength);

            header.AddRange(new byte[RstmHeaderLength - header.Count]);

            return header.ToArray();
        }

        private byte[] GetHeadChunk()
        {
            var chunk = new List<byte>();

            chunk.Add32("HEAD");
            chunk.Add32BE(HeadChunkLength);
            chunk.AddRange(GetHeadChunkHeader());
            chunk.AddRange(GetHeadChunk1());
            chunk.AddRange(GetHeadChunk2());
            chunk.AddRange(GetHeadChunk3());

            chunk.AddRange(new byte[HeadChunkLength - chunk.Count]);

            return chunk.ToArray();
        }

        private byte[] GetHeadChunkHeader()
        {
            var chunk = new List<byte>();

            chunk.Add32BE(0x01000000);
            chunk.Add32BE(HeadChunkTableLength); //Chunk 1 offset
            chunk.Add32BE(0x01000000);
            chunk.Add32BE(HeadChunkTableLength + HeadChunk1Length); //Chunk 1 offset
            chunk.Add32BE(0x01000000);
            chunk.Add32BE(HeadChunkTableLength + HeadChunk1Length + HeadChunk2Length); //Chunk 3 offset

            return chunk.ToArray();
        }

        private byte[] GetHeadChunk1()
        {
            var chunk = new List<byte>();

            chunk.Add(Codec);
            chunk.Add(Looping);
            chunk.Add((byte)NumChannels);
            chunk.Add(0); //padding
            chunk.Add16BE(AudioStream.SampleRate);
            chunk.Add16BE(0); //padding
            chunk.Add32BE(AudioStream.LoopStart);
            chunk.Add32BE(AudioStream.NumSamples);
            chunk.Add32BE(AudioDataOffset);
            chunk.Add32BE(InterleaveCount);
            chunk.Add32BE(InterleaveSize);
            chunk.Add32BE(SamplesPerInterleave);
            chunk.Add32BE(LastBlockSizeWithoutPadding);
            chunk.Add32BE(LastBlockSamples);
            chunk.Add32BE(LastBlockSize);
            chunk.Add32BE(SamplesPerAdpcEntry);
            chunk.Add32BE(BytesPerAdpcEntry);

            return chunk.ToArray();
        }

        //Not sure what this chunk is for
        private byte[] GetHeadChunk2()
        {
            var chunk = new List<byte>();

            chunk.Add32BE(0x01000000);
            chunk.Add32BE(0x01000000);
            chunk.Add32BE(0x58);
            chunk.Add32BE(NumChannels == 1 ? 0x01000000 : 0x02000100);

            return chunk.ToArray();
        }

        private byte[] GetHeadChunk3()
        {
            var chunk = new List<byte>();

            chunk.Add((byte)NumChannels);
            chunk.Add(0); //padding
            chunk.Add16BE(0); //padding

            int baseOffset = HeadChunkTableLength + HeadChunk1Length + HeadChunk2Length + 4;
            int offsetTableLength = NumChannels * 8;

            if (AudioStream.Looping)
            {
                Parallel.ForEach(AudioStream.Channels, x => x.SetLoopContext(AudioStream.LoopStart));
            }

            for (int i = 0; i < NumChannels; i++)
            {
                chunk.Add32BE(0x01000000);
                chunk.Add32BE(baseOffset + offsetTableLength + ChannelInfoLength * i);
            }

            for (int i = 0; i < NumChannels; i++)
            {
                IAdpcmChannel channel = AudioStream.Channels[i];
                chunk.Add32BE(0x01000000);
                chunk.Add32BE(baseOffset + offsetTableLength + ChannelInfoLength * i + 8);
                chunk.AddRange(channel.Coefs.SelectMany(x => x.ToBytesBE()));
                chunk.Add16BE(channel.Gain);
                chunk.Add16BE(channel.AudioData.First());
                chunk.Add16BE(channel.Hist1);
                chunk.Add16BE(channel.Hist2);
                chunk.Add16BE(channel.LoopPredScale);
                chunk.Add16BE(channel.LoopHist1);
                chunk.Add16BE(channel.LoopHist2);
                chunk.Add16(0);
            }

            return chunk.ToArray();
        }

        private byte[] GetAdpcChunk()
        {
            var chunk = new List<byte>();

            chunk.Add32("ADPC");
            chunk.Add32BE(AdpcChunkLength);
            chunk.AddRange(new byte[8]); //Pad to 0x10 bytes

            chunk.AddRange(Encode.BuildAdpcTable(AudioStream.Channels, SamplesPerAdpcEntry, NumAdpcEntries));

            chunk.AddRange(new byte[AdpcChunkLength - chunk.Count]);

            return chunk.ToArray();
        }

        private byte[] GetDataChunk()
        {
            var chunk = new List<byte>();

            chunk.Add32("DATA");
            chunk.Add32BE(DataChunkLength);
            chunk.Add32BE(0x18);
            chunk.AddRange(new byte[0x14]); //Pad to 0x20 bytes

            var channels = AudioStream.Channels.Select(x => x.AudioData.ToArray()).ToArray();
            chunk.AddRange(channels.Interleave(InterleaveSize, LastBlockSize));

            return chunk.ToArray();
        }

        public void ReadBrstmFile(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "RSTM")
                {
                    throw new InvalidDataException("File has no RSTM header");
                }

                var structure = new BrstmStructure();

                reader.BaseStream.Position = 8;
                structure.FileLength = reader.ReadInt32BE();
                structure.RstmHeaderLength = reader.ReadInt16BE();
                reader.BaseStream.Position += 2;

                structure.HeadChunkOffset = reader.ReadInt32BE();
                structure.HeadChunkLengthRstm = reader.ReadInt32BE();
                structure.AdpcChunkOffset = reader.ReadInt32BE();
                structure.AdpcChunkLengthRstm = reader.ReadInt32BE();
                structure.DataChunkOffset = reader.ReadInt32BE();
                structure.DataChunkLengthRstm = reader.ReadInt32BE();

                reader.BaseStream.Position = structure.HeadChunkOffset;
                byte[] headChunk = reader.ReadBytes(structure.HeadChunkLengthRstm);
                ParseHeadChunk(headChunk, structure);

                SamplesPerInterleave = structure.SamplesPerInterleave;
                SamplesPerAdpcEntry = structure.SamplesPerAdpcEntry;
                RstmHeaderLength = structure.RstmHeaderLength;
                HeadChunk2Length = structure.HeadChunk2Length;

                AudioStream = new AdpcmStream(structure.NumSamples, structure.SampleRate);
                if (structure.Looping)
                {
                    AudioStream.SetLoop(structure.LoopStart, structure.NumSamples);
                }

                ParseDataChunk(stream, structure);
            }
        }

        private static void ParseHeadChunk(byte[] head, BrstmStructure structure)
        {
            int baseOffset = 8;

            if (Encoding.UTF8.GetString(head, 0, 4) != "HEAD")
            {
                throw new InvalidDataException("Unknown or invalid HEAD chunk");
            }

            using (var reader = new BinaryReader(new MemoryStream(head)))
            {
                reader.BaseStream.Position = 4;
                if (reader.ReadInt32BE() != head.Length)
                {
                    throw new InvalidDataException("HEAD chunk stated length does not match actual length");
                }

                reader.BaseStream.Position += 4;
                structure.HeadChunk1Offset = reader.ReadInt32BE();
                reader.BaseStream.Position += 4;
                structure.HeadChunk2Offset = reader.ReadInt32BE();
                reader.BaseStream.Position += 4;
                structure.HeadChunk3Offset = reader.ReadInt32BE();

                reader.BaseStream.Position = structure.HeadChunk1Offset + baseOffset;
                byte[] headChunk1 = reader.ReadBytes(structure.HeadChunk1Length);

                reader.BaseStream.Position = structure.HeadChunk3Offset + baseOffset;
                byte[] headChunk3 = reader.ReadBytes(structure.HeadChunk3Length);

                ParseHeadChunk1(headChunk1, structure);
                ParseHeadChunk3(headChunk3, structure);
            }
        }

        private static void ParseHeadChunk1(byte[] chunk, BrstmStructure structure)
        {
            using (var reader = new BinaryReader(new MemoryStream(chunk)))
            {
                structure.Codec = reader.ReadByte();
                if (structure.Codec != 2) //4-bit ADPCM codec
                {
                    throw new InvalidDataException("File must contain 4-bit ADPCM encoded audio");
                }

                structure.Looping = reader.ReadByte() == 1;
                structure.NumChannelsChunk1 = reader.ReadByte();
                reader.BaseStream.Position += 1;

                structure.SampleRate = reader.ReadInt16BE();
                reader.BaseStream.Position += 2;

                structure.LoopStart = reader.ReadInt32BE();
                structure.NumSamples = reader.ReadInt32BE();

                structure.AudioDataOffset = reader.ReadInt32BE();
                structure.InterleaveCount = reader.ReadInt32BE();
                structure.InterleaveSize = reader.ReadInt32BE();
                structure.SamplesPerInterleave = reader.ReadInt32BE();
                structure.LastBlockSizeWithoutPadding = reader.ReadInt32BE();
                structure.LastBlockSamples = reader.ReadInt32BE();
                structure.LastBlockSize = reader.ReadInt32BE();
                structure.SamplesPerAdpcEntry = reader.ReadInt32BE();
            }
        }

        private static void ParseHeadChunk3(byte[] chunk, BrstmStructure structure)
        {
            using (var reader = new BinaryReader(new MemoryStream(chunk)))
            {
                structure.NumChannelsChunk3 = reader.ReadByte();
                reader.BaseStream.Position += 3;

                for (int i = 0; i < structure.NumChannelsChunk3; i++)
                {
                    var channel = new ChannelInfo();
                    reader.BaseStream.Position += 4;
                    channel.Offset = reader.ReadInt32BE();
                    structure.Channels.Add(channel);
                }

                int baseOffset = structure.HeadChunk3Offset;
                foreach (ChannelInfo channel in structure.Channels)
                {
                    reader.BaseStream.Position = channel.Offset - structure.HeadChunk3Offset + 4;
                    int coefsOffset = reader.ReadInt32BE();
                    reader.BaseStream.Position = coefsOffset - structure.HeadChunk3Offset;

                    channel.Coefs = Enumerable.Range(0, 16).Select(x => reader.ReadInt16BE()).ToArray();
                    channel.Gain = reader.ReadInt16BE();
                    channel.PredScale = reader.ReadInt16BE();
                    channel.Hist1 = reader.ReadInt16BE();
                    channel.Hist2 = reader.ReadInt16BE();
                    channel.LoopPredScale = reader.ReadInt16BE();
                    channel.LoopHist1 = reader.ReadInt16BE();
                    channel.LoopHist2 = reader.ReadInt16BE();
                }
            }
        }

        private void ParseDataChunk(Stream chunk, BrstmStructure structure)
        {
            using (var reader = new BinaryReader(chunk))
            {
                reader.BaseStream.Position = structure.DataChunkOffset;
                if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "DATA")
                {
                    throw new InvalidDataException("Unknown or invalid DATA chunk");
                }
                structure.DataChunkLength = reader.ReadInt32BE();

                reader.BaseStream.Position = structure.AudioDataOffset;
                int audioDataLength = structure.DataChunkLength - (structure.AudioDataOffset - structure.DataChunkOffset);

                List<AdpcmChannel> channels = structure.Channels.Select(channelInfo =>
                    new AdpcmChannel(structure.NumSamples)
                    {
                        Coefs = channelInfo.Coefs,
                        Gain = channelInfo.Gain,
                        Hist1 = channelInfo.Hist1,
                        Hist2 = channelInfo.Hist2
                    })
                    .ToList();

                byte[] audioData = reader.ReadBytes(audioDataLength);

                byte[][] deInterleavedAudioData = audioData.DeInterleave(structure.InterleaveSize, structure.NumChannelsChunk1,
                    structure.LastBlockSize, GetBytesForAdpcmSamples(structure.NumSamples));

                for (int c = 0; c < structure.NumChannelsChunk1; c++)
                {
                    channels[c].AudioByteArray = deInterleavedAudioData[c];
                }

                foreach (AdpcmChannel channel in channels)
                {
                    AudioStream.Channels.Add(channel);
                }
            }
        }

        private class BrstmStructure
        {
            public int FileLength { get; set; }
            public int RstmHeaderLength { get; set; }
            public int HeadChunkOffset { get; set; }
            public int HeadChunkLengthRstm { get; set; }
            public int AdpcChunkOffset { get; set; }
            public int AdpcChunkLengthRstm { get; set; }
            public int DataChunkOffset { get; set; }
            public int DataChunkLengthRstm { get; set; }

            public int HeadChunkLength { get; set; }
            public int HeadChunk1Offset { get; set; }
            public int HeadChunk2Offset { get; set; }
            public int HeadChunk3Offset { get; set; }
            public int HeadChunk1Length => HeadChunk2Offset - HeadChunk1Offset;
            public int HeadChunk2Length => HeadChunk3Offset - HeadChunk2Offset;
            public int HeadChunk3Length => AdpcChunkOffset - HeadChunk3Offset;

            public int Codec { get; set; }
            public bool Looping { get; set; }
            public int NumChannelsChunk1 { get; set; }
            public int SampleRate { get; set; }
            public int LoopStart { get; set; }
            public int NumSamples { get; set; }
            public int AudioDataOffset { get; set; }
            public int InterleaveCount { get; set; }
            public int InterleaveSize { get; set; }
            public int SamplesPerInterleave { get; set; }
            public int LastBlockSizeWithoutPadding { get; set; }
            public int LastBlockSamples { get; set; }
            public int LastBlockSize { get; set; }
            public int SamplesPerAdpcEntry { get; set; }

            public int NumChannelsChunk3 { get; set; }
            public List<ChannelInfo> Channels { get; set; } = new List<ChannelInfo>();

            public int DataChunkLength { get; set; }
        }

        private class ChannelInfo
        {
            public int Offset { get; set; }

            public short[] Coefs { get; set; }

            public short Gain { get; set; }
            public short PredScale { get; set; }
            public short Hist1 { get; set; } = 0;
            public short Hist2 { get; set; } = 0;

            public short LoopPredScale { get; set; }
            public short LoopHist1 { get; set; }
            public short LoopHist2 { get; set; }
        }
    }
}
