using System.IO;
using VGAudio.Codecs.Atrac9;
using VGAudio.Utilities;
using VGAudio.Utilities.Riff;

namespace VGAudio.Containers.At9
{
    internal class At9DataChunk : RiffSubChunk
    {
        public int FrameCount { get; set; }
        public byte[][] AudioData { get; set; }

        public At9DataChunk(RiffParser parser, BinaryReader reader) : base(reader)
        {
            // Do not trust the BlockAlign field in the fmt chunk to equal the superframe size.
            // Some AT9 files have an invalid number in there.
            // Calculate the size using the ATRAC9 DataConfig instead.

            At9WaveExtensible ext = parser.GetSubChunk<WaveFmtChunk>("fmt ")?.Ext as At9WaveExtensible ??
                                    throw new InvalidDataException("fmt chunk must come before data chunk");

            At9FactChunk fact = parser.GetSubChunk<At9FactChunk>("fact") ??
                       throw new InvalidDataException("fact chunk must come before data chunk");

            var config = new Atrac9Config(ext.ConfigData);
            FrameCount = (fact.SampleCount + fact.EncoderDelaySamples).DivideByRoundUp(config.SuperframeSamples);
            int dataSize = FrameCount * config.SuperframeBytes;

            if (dataSize > reader.BaseStream.Length - reader.BaseStream.Position)
            {
                throw new InvalidDataException("Required AT9 length is greater than the number of bytes remaining in the file.");
            }

            AudioData = reader.BaseStream.DeInterleave(dataSize, config.SuperframeBytes, FrameCount);
        }

        public static At9DataChunk ParseAt9(RiffParser parser, BinaryReader reader) => new At9DataChunk(parser, reader);
    }
}
