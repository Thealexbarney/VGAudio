using System.Collections.Generic;

namespace DspAdpcm.Lib.Adpcm.Formats
{
    public class BrstmStructure
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
        public int HeadChunk3Length => HeadChunkLength - HeadChunk3Offset;

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

        public Brstm.BrstmHeaderType HeaderType { get; set; } = Brstm.BrstmHeaderType.SSBB;
        public List<AdpcmTrack> Tracks { get; set; } = new List<AdpcmTrack>();

        public int NumChannelsChunk3 { get; set; }
        public List<Brstm.ChannelInfo> Channels { get; set; } = new List<Brstm.ChannelInfo>();

        public int AdpcChunkLength { get; set; }
        public int AdpcTableLength { get; set; }
        public short[][] SeekTable { get; set; }
        public Brstm.SeekTableType SeekTableType { get; set; } = Brstm.SeekTableType.Standard;

        public int DataChunkLength { get; set; }
    }
}