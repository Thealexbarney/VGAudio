using System.Collections.Generic;
using VGAudio.Containers.NintendoWare.Structures;
using VGAudio.Utilities;

namespace VGAudio.Containers.NintendoWare
{
    public class BxstmStructure
    {
        /// <summary>
        /// The size of the entire file.
        /// </summary>
        public int FileSize { get; set; }
        public Endianness Endianness { get; set; }

        /// <summary>
        /// The size of the file header.
        /// </summary>
        public int HeaderSize { get; set; }
        /// <summary>
        /// The version listed in the header.
        /// </summary>
        public NwVersion Version { get; set; }
        /// <summary>
        /// The number of blocks listed in the header.
        /// </summary>
        public int BlockCount { get; set; }

        /// <summary>
        /// Information about the data blocks in the file.
        /// Used in BRSTM files.
        /// </summary>
        public BrstmHeader BrstmHeader { get; set; }
        /// <summary>
        /// Information about the data blocks in the file.
        /// Used in BCSTM and BFSTM files.
        /// </summary>
        public List<SizedReference> Blocks { get; set; } = new List<SizedReference>();

        public StreamInfo StreamInfo { get; set; }
        public TrackInfo TrackInfo { get; set; }
        public ChannelInfo ChannelInfo { get; set; }

        /// <summary>The audio regions in the file.
        /// Used in BCSTM and BFSTM files.</summary>
        public List<RegionInfo> Regions { get; set; }
        public List<PrefetchData> PrefetchData { get; set; }

        /// <summary>
        /// Specifies whether the seek table is full
        /// length, or a truncated table used in some
        /// games including Pokémon Battle Revolution
        /// and Mario Party 8. 
        /// Used in BRSTM files.
        /// </summary>
        public BrstmSeekTableType BrstmSeekTableType { get; set; }

        /// <summary>
        /// The seek table containing PCM samples
        /// from throughout the audio stream.
        /// </summary>
        public short[][] SeekTable { get; set; }
        internal byte[][] AudioData { get; set; }
    }
}