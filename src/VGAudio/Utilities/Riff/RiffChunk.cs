using System.IO;

namespace VGAudio.Utilities.Riff
{
    public class RiffChunk
    {
        public string ChunkId { get; set; }
        public int Size { get; set; }
        public string Type { get; set; }

        public static RiffChunk Parse(BinaryReader reader)
        {
            var chunk = new RiffChunk
            {
                ChunkId = reader.ReadUTF8(4),
                Size = reader.ReadInt32(),
                Type = reader.ReadUTF8(4)
            };

            if (chunk.ChunkId != "RIFF")
            {
                throw new InvalidDataException("Not a valid RIFF file");
            }

            return chunk;
        }
    }
}
