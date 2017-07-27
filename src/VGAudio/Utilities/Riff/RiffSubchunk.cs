using System.IO;

namespace VGAudio.Utilities.Riff
{
    public class RiffSubChunk
    {
        public string SubChunkId { get; set; }
        public int SubChunkSize { get; set; }
        public byte[] Extra { get; set; }

        public RiffSubChunk(BinaryReader reader)
        {
            SubChunkId = reader.ReadUTF8(4);
            SubChunkSize = reader.ReadInt32();
        }
    }
}
