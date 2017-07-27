using System;
using System.IO;

namespace VGAudio.Utilities.Riff
{
    public class WaveFormatExtensible
    {
        public int Size { get; set; }
        public int ValidBitsPerSample { get; set; }
        public int SamplesPerBlock
        {
            get => ValidBitsPerSample;
            set => ValidBitsPerSample = value;
        }
        public uint ChannelMask { get; set; }
        public Guid SubFormat { get; set; }
        public byte[] Extra { get; set; }

        protected WaveFormatExtensible(BinaryReader reader)
        {
            Size = reader.ReadInt16();

            ValidBitsPerSample = reader.ReadInt16();
            ChannelMask = reader.ReadUInt32();
            SubFormat = new Guid(reader.ReadBytes(16));
        }

        public static WaveFormatExtensible Parse(RiffParser parser, BinaryReader reader) =>
            new WaveFormatExtensible(reader);
    }
}
