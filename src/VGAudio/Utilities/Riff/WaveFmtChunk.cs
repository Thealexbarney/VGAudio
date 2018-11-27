using System;
using System.IO;

namespace VGAudio.Utilities.Riff
{
    public class WaveFmtChunk : RiffSubChunk
    {
        public int FormatTag { get; set; }
        public int ChannelCount { get; set; }
        public int SampleRate { get; set; }
        public int AvgBytesPerSec { get; set; }
        public int BlockAlign { get; set; }
        public int BitsPerSample { get; set; }
        public WaveFormatExtensible Ext { get; set; }

        protected WaveFmtChunk(RiffParser parser, BinaryReader reader) : base(reader)
        {
            FormatTag = reader.ReadUInt16();
            ChannelCount = reader.ReadInt16();
            SampleRate = reader.ReadInt32();
            AvgBytesPerSec = reader.ReadInt32();
            BlockAlign = reader.ReadInt16();
            BitsPerSample = reader.ReadInt16();

            if (FormatTag == WaveFormatTags.WaveFormatExtensible && parser.FormatExtensibleParser != null)
            {
                long startOffset = reader.BaseStream.Position + 2;
                Ext = parser.FormatExtensibleParser(parser, reader);

                long endOffset = startOffset + Ext.Size;
                int remainingBytes = (int)Math.Max(endOffset - reader.BaseStream.Position, 0);
                Ext.Extra = reader.ReadBytes(remainingBytes);
            }
        }

        public static WaveFmtChunk Parse(RiffParser parser, BinaryReader reader) => new WaveFmtChunk(parser, reader);
    }
}
