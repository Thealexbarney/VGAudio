using System.IO;

namespace VGAudio.Utilities.Riff
{
    public class WaveDataChunk : RiffSubChunk
    {
        public byte[] Data { get; set; }

        protected WaveDataChunk(RiffParser parser, BinaryReader reader) : base(reader)
        {
            if (parser.ReadDataChunk)
            {
                Data = reader.ReadBytes(SubChunkSize);
            }
        }

        public static WaveDataChunk Parse(RiffParser parser, BinaryReader reader) => new WaveDataChunk(parser, reader);
    }
}
