using System.IO;
using VGAudio.Utilities.Riff;

namespace VGAudio.Containers.At9
{
    internal class At9WaveExtensible : WaveFormatExtensible
    {
        public int VersionInfo { get; set; }
        public byte[] ConfigData { get; set; }
        public int Reserved { get; set; }

        protected At9WaveExtensible(BinaryReader reader) : base(reader)
        {
            VersionInfo = reader.ReadInt32();
            ConfigData = reader.ReadBytes(4);
            Reserved = reader.ReadInt32();
        }

        public static At9WaveExtensible ParseAt9(RiffParser parser, BinaryReader reader) =>
            new At9WaveExtensible(reader);
    }
}
