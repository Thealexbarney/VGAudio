using System.IO;

namespace VGAudio.Utilities.Riff
{
    public class WaveFactChunk : RiffSubChunk
    {
        public int SampleCount { get; set; }

        protected WaveFactChunk(BinaryReader reader) : base(reader)
        {
            SampleCount = reader.ReadInt32();
        }

        public static WaveFactChunk Parse(RiffParser parser, BinaryReader reader) => new WaveFactChunk(reader);
    }
}
