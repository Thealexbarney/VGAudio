using System.Collections.Generic;
using VGAudio.Containers.Bxstm.Structures;
using VGAudio.Formats;
using VGAudio.Formats.GcAdpcm;
using VGAudio.Utilities;

namespace VGAudio.Containers.Bxstm
{
    public abstract class BxstmStructure
    {
        /// <summary>
        /// The size of the entire file.
        /// </summary>
        public int FileSize { get; set; }
        public Endianness Endianness { get; set; }

        public List<SizedReference> Sections { get; set; } = new List<SizedReference>();

        public StreamInfo StreamInfo { get; set; }
        public TrackInfo TrackInfo { get; set; }

        /// <summary>
        /// The size of the file header.
        /// </summary>
        public int HeaderSize { get; set; }
        /// <summary>
        /// The number of sections listed in the header.
        /// </summary>
        public int HeaderSections { get; set; }

        /// <summary>
        /// The offset of the HEAD/INFO chunk.
        /// </summary>
        public int InfoChunkOffset { get; set; }
        /// <summary>
        /// The size of the HEAD/INFO chunk as stated in the header.
        /// </summary>
        public int InfoChunkSizeHeader { get; set; }
        /// <summary>
        /// The offset of the ADPC/SEEK chunk.
        /// </summary>
        public int SeekChunkOffset { get; set; }
        /// <summary>
        /// The size of the ADPC/SEEK chunk as stated in the header.
        /// </summary>
        public int SeekChunkSizeHeader { get; set; }
        /// <summary>
        /// The offset of the DATA chunk.
        /// </summary>
        public int DataChunkOffset { get; set; }
        /// <summary>
        /// The size of the DATA chunk as stated in the header.
        /// </summary>
        public int DataChunkSizeHeader { get; set; }

        /// <summary>
        /// The size of the HEAD/INFO chunk as stated in the
        /// HEAD/INFO chunk header.
        /// </summary>
        public int InfoChunkSize { get; set; }

        /// <summary>
        /// The offset of part 2 of the HEAD/INFO chunk.
        /// </summary>
        public int InfoChunk2Offset { get; set; }
        /// <summary>
        /// The offset of part 3 of the HEAD/INFO chunk.
        /// </summary>
        public int InfoChunk3Offset { get; set; }

        /// <summary>
        /// The size of the ADPC/SEEK chunk as stated in the
        /// ADPC/SEEK chunk header.
        /// </summary>
        public int SeekChunkSize { get; set; }

        /// <summary>
        /// The size of the DATA chunk as stated in the
        /// DATA chunk header.
        /// </summary>
        public int DataChunkSize { get; set; }

        /// <summary>
        /// The version listed in the header.
        /// </summary>
        public NwVersion Version { get; set; }

        /// <summary>
        /// A list of all tracks defined in the file.
        /// </summary>
        public List<AudioTrack> Tracks { get; set; } = new List<AudioTrack>();
        /// <summary>
        /// The ADPCM information for each channel.
        /// </summary>
        public List<GcAdpcmChannelInfo> Channels { get; set; } = new List<GcAdpcmChannelInfo>();

        /// <summary>
        /// The size of the seek table in the file.
        /// </summary>
        public int SeekTableSize { get; set; }
        /// <summary>
        /// The seek table containing PCM samples
        /// from throughout the audio stream.
        /// </summary>
        public short[][] SeekTable { get; set; }
        internal byte[][] AudioData { get; set; }
    }
}