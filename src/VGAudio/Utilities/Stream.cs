using System.IO;
using System.Text;

namespace VGAudio.Utilities
{
    internal static class GetStream
    {
        public static BinaryReader GetBinaryReader(Stream stream) => new BinaryReader(stream, Encoding.UTF8, true);
        public static BinaryReaderBE GetBinaryReaderBE(Stream stream) => new BinaryReaderBE(stream, Encoding.UTF8, true);
        public static BinaryWriter GetBinaryWriter(Stream stream) => new BinaryWriter(stream, Encoding.UTF8, true);
        public static BinaryWriterBE GetBinaryWriterBE(Stream stream) => new BinaryWriterBE(stream, Encoding.UTF8, true);
    }
}
