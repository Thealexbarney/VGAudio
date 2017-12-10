using System.IO;
using VGAudio.Utilities.Riff;

namespace VGAudio.Containers.At9
{
    internal class At9FactChunk : WaveFactChunk
    {
        public int InputOverlapDelaySamples { get; set; }
        public int EncoderDelaySamples { get; set; }

        protected At9FactChunk(BinaryReader reader) : base(reader)
        {
            InputOverlapDelaySamples = reader.ReadInt32();
            EncoderDelaySamples = reader.ReadInt32();
        }

        public static At9FactChunk ParseAt9(RiffParser parser, BinaryReader reader) => new At9FactChunk(reader);
    }
}
