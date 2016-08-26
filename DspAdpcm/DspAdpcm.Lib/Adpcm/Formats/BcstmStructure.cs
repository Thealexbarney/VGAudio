using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DspAdpcm.Lib.Adpcm.Formats
{
    /// <summary>
    /// Defines the structure and metadata
    /// of a BCSTM file.
    /// </summary>
    public class BcstmStructure
    {
        /// <summary>
        /// The length of the entire BCSTM file.
        /// </summary>
        public int FileLength { get; set; }
        /// <summary>
        /// The length of the CSTM header.
        /// </summary>
        public int CstmHeaderLength { get; set; }
        /// <summary>
        /// The number of sections listed in the CSTM header.
        /// </summary>
        public int CstmHeaderSections { get; set; }
        /// <summary>
        /// The offset of the INFO chunk.
        /// </summary>
        public int InfoChunkOffset { get; set; }
        /// <summary>
        /// The length of the INFO chunk as stated in the
        /// CSTM header.
        /// </summary>
        public int InfoChunkLengthCstm { get; set; }
        /// <summary>
        /// The offset of the SEEK chunk.
        /// </summary>
        public int SeekChunkOffset { get; set; }
        /// <summary>
        /// The length of the SEEK chunk as stated in the
        /// CSTM header.
        /// </summary>
        public int SeekChunkLengthCstm { get; set; }
        /// <summary>
        /// The offset of the DATA chunk.
        /// </summary>
        public int DataChunkOffset { get; set; }
        /// <summary>
        /// The length of the DATA chunk as stated in the
        /// CSTM header.
        /// </summary>
        public int DataChunkLengthCstm { get; set; }

        /// <summary>
        /// The length of the INFO chunk as stated in the
        /// INFO chunk header.
        /// </summary>
        public int InfoChunkLength { get; set; }
        /// <summary>
        /// The offset of part 1 of the INFO chunk.
        /// </summary>
        public int InfoChunk1Offset { get; set; }
        /// <summary>
        /// The offset of part 2 of the INFO chunk.
        /// </summary>
        public int InfoChunk2Offset { get; set; }
        /// <summary>
        /// The offset of part 3 of the INFO chunk.
        /// </summary>
        public int InfoChunk3Offset { get; set; }

        /// <summary>
        /// The audio codec.
        /// </summary>
        public BcstmCodec Codec { get; set; }
        /// <summary>
        /// This flag is set if the BCSTM loops.
        /// </summary>
        public bool Looping { get; set; }
        /// <summary>
        /// The number of channels as stated in part 1
        /// of the INFO chunk.
        /// </summary>
        public int NumChannelsPart1 { get; set; }
        /// <summary>
        /// The sample rate of the audio.
        /// </summary>
        public int SampleRate { get; set; }
        /// <summary>
        /// The start loop point in samples.
        /// </summary>
        public int LoopStart { get; set; }
        /// <summary>
        /// The start loop end in samples.
        /// </summary>
        public int LoopEnd { get; set; }
        /// <summary>
        /// The number of samples in the BCSTM.
        /// </summary>
        public int NumSamples { get; set; }
        /// <summary>
        /// The total count of interleaved audio data blocks.
        /// </summary>
        public int InterleaveCount { get; set; }
        /// <summary>
        /// The number of bytes per channel in each
        /// interleaved audio data block.
        /// </summary>
        public int InterleaveSize { get; set; }
        /// <summary>
        /// The number of samples per channel in each
        /// interleaved audio data block.
        /// </summary>
        public int SamplesPerInterleave { get; set; }
        /// <summary>
        /// The number of bytes per channel in the final
        /// interleaved audio data block, not including
        /// the padding at the end of each channel.
        /// </summary>
        public int LastBlockSizeWithoutPadding { get; set; }
        /// <summary>
        /// he number of samples per channel in the final
        /// interleaved audio data block.
        /// </summary>
        public int LastBlockSamples { get; set; }
        /// <summary>
        /// The number of bytes per channel in the final
        /// interleaved audio data block, including
        /// the padding at the end of each channel.
        /// </summary>
        public int LastBlockSize { get; set; }
        /// <summary>
        /// The number of bytes per seek table entry.
        /// </summary>
        public int BytesPerSeekTableEntry { get; set; }
        /// <summary>
        /// The number of samples per seek table entry.
        /// </summary>
        public int SamplesPerSeekTableEntry { get; set; }

        /// <summary>
        /// Specifies whether the BCSTM includes an extra chunk in the header
        /// after the stream info and before the track offset table.
        /// The purpose of this chunk is unknown.
        /// </summary>
        public bool InfoPart1Extra { get; set; }

        /// <summary>
        /// A list of all tracks defined in the BCSTM.
        /// </summary>
        public List<AdpcmTrack> Tracks { get; set; } = new List<AdpcmTrack>();

        /// <summary>
        /// Specifies whether the BCSTM lists the tracks
        /// included in it.
        /// </summary>
        public bool IncludeTracks { get; set; }

        /// <summary>
        /// The number of channels as stated in part 3
        /// of the INFO chunk.
        /// </summary>
        public int NumChannelsPart3 { get; set; }
        /// <summary>
        /// The ADPCM information for each channel.
        /// </summary>
        public List<BcstmChannelInfo> Channels { get; set; } = new List<BcstmChannelInfo>();

        /// <summary>
        /// The length of the SEEK chunk as stated in the
        /// SEEK chunk header.
        /// </summary>
        public int SeekChunkLength { get; set; }
        /// <summary>
        /// The length of the seek table in the
        /// SEEK chunk.
        /// </summary>
        public int SeekTableLength { get; set; }
        /// <summary>
        /// The seek table containing PCM samples
        /// from throughout the audio stream.
        /// </summary>
        public short[][] SeekTable { get; set; }

        /// <summary>
        /// The length of the DATA chunk as stated in the
        /// DATA chunk header.
        /// </summary>
        public int DataChunkLength { get; set; }
        /// <summary>
        /// The version listed in the CSTM header.
        /// </summary>
        public int Version { get; set; }
    }

    /// <summary>
    /// The different audio codecs used in BCSTM files.
    /// </summary>
    public enum BcstmCodec
    {
        /// <summary>
        /// Big-endian, 8-bit PCM
        /// </summary>
        Pcm8Bit = 0,
        /// <summary>
        /// Big-endian, 16-bit PCM
        /// </summary>
        Pcm16Bit = 1,
        /// <summary>
        /// Nintendo's 4-Bit ADPCM
        /// </summary>
        Adpcm = 2
    }

    /// <summary>
    /// Defines the ADPCM information for a single
    /// ADPCM channel.
    /// </summary>
    public class BcstmChannelInfo : AdpcmChannelInfo
    {
        /// <summary>
        /// The offset of the channel information. 
        /// Used in a BCSTM header.
        /// </summary>
        public int Offset { get; set; }
    }
}
