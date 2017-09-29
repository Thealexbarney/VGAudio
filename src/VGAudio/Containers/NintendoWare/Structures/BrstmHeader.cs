using System.IO;

namespace VGAudio.Containers.NintendoWare.Structures
{
    public class BrstmHeader
    {
        /// <summary>
        /// The offset of the HEAD block.
        /// </summary>
        public int HeadBlockOffset { get; set; }
        /// <summary>
        /// The size of the HEAD block as stated in the header.
        /// </summary>
        public int HeadBlockSize { get; set; }
        /// <summary>
        /// The offset of the ADPC block.
        /// </summary>
        public int SeekBlockOffset { get; set; }
        /// <summary>
        /// The size of the ADPC block as stated in the header.
        /// </summary>
        public int SeekBlockSize { get; set; }
        /// <summary>
        /// The offset of the DATA block.
        /// </summary>
        public int DataBlockOffset { get; set; }
        /// <summary>
        /// The size of the DATA block as stated in the header.
        /// </summary>
        public int DataBlockSize { get; set; }

        public static BrstmHeader Read(BinaryReader reader)
        {
            return new BrstmHeader
            {
                HeadBlockOffset = reader.ReadInt32(),
                HeadBlockSize = reader.ReadInt32(),
                SeekBlockOffset = reader.ReadInt32(),
                SeekBlockSize = reader.ReadInt32(),
                DataBlockOffset = reader.ReadInt32(),
                DataBlockSize = reader.ReadInt32()
            };
        }

        public static BrstmHeader ReadBrwav(BinaryReader reader)
        {
            return new BrstmHeader
            {
                HeadBlockOffset = reader.ReadInt32(),
                HeadBlockSize = reader.ReadInt32(),
                DataBlockOffset = reader.ReadInt32(),
                DataBlockSize = reader.ReadInt32()
            };
        }
    }
}
