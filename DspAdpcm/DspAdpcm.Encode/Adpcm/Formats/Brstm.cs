using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static DspAdpcm.Encode.Helpers;

namespace DspAdpcm.Encode.Adpcm.Formats
{
    public class Brstm
    {
        public IAdpcmStream AudioStream { get; set; }
        
        private int NumSamples => AudioStream.NumSamples;
        private int NumChannels => AudioStream.Channels.Count;
        private byte Codec { get; } = 2; // 4-bit ADPCM
        private byte Looping => (byte)(AudioStream.Looping ? 1 : 0);
        private int AudioDataOffset => DataChunkOffset + 0x20;
        private int InterleaveSize => GetBytesForAdpcmSamples(SamplesPerInterleave);
        private int SamplesPerInterleave => 0x3800;
        private int InterleaveCount => (NumSamples / SamplesPerInterleave) + (LastBlockSamples == 0 ? 0 : 1);
        private int LastBlockSizeWithoutPadding => GetBytesForAdpcmSamples(LastBlockSamples);
        private int LastBlockSamples => NumSamples % SamplesPerInterleave;
        private int LastBlockSize => GetNextMultiple(LastBlockSizeWithoutPadding, 0x20);
        private int SamplesPerAdpcEntry => 0x3800;
        private int NumAdpcEntries => (NumSamples / SamplesPerAdpcEntry) - (LastBlockSamples == 0 ? 1 : 0);
        private int BytesPerAdpcEntry => 4; //Or is it bits per sample?

        private int RstmHeaderLength => 0x40;

        private int HeadChunkOffset => RstmHeaderLength;
        private int HeadChunkLength => GetNextMultiple(HeadChunkHeaderLength + HeadChunkTableLength +
            HeadChunk1Length + HeadChunk2Length + HeadChunk3Length, 0x20);
        private int HeadChunkHeaderLength = 8;
        private int HeadChunkTableLength => 8 * 3;
        private int HeadChunk1Length => 0x34;
        private int HeadChunk2Length => 0x10;
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

            AudioStream = stream;
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
    }
}
