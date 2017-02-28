using System.IO;
using System.Text;
using VGAudio.Utilities;

namespace VGAudio.Compatibility
{
    internal static class GetStream
    {
#if !(NET20 || NET35 || NET40)
        public static BinaryReader GetBinaryReader(Stream stream) => new BinaryReader(stream, Encoding.UTF8, true);
        public static BinaryReaderBE GetBinaryReaderBE(Stream stream) => new BinaryReaderBE(stream, Encoding.UTF8, true);
        public static BinaryWriter GetBinaryWriter(Stream stream) => new BinaryWriter(stream, Encoding.UTF8, true);
        public static BinaryWriterBE GetBinaryWriterBE(Stream stream) => new BinaryWriterBE(stream, Encoding.UTF8, true);
#else
        public static BinaryReader GetBinaryReader(Stream stream) => new BinaryReader(new StreamNoDispose(stream));
        public static BinaryReaderBE GetBinaryReaderBE(Stream stream) => new BinaryReaderBE(new StreamNoDispose(stream));
        public static BinaryWriter GetBinaryWriter(Stream stream) => new BinaryWriter(new StreamNoDispose(stream));
        public static BinaryWriterBE GetBinaryWriterBE(Stream stream) => new BinaryWriterBE(new StreamNoDispose(stream));
#endif
    }

#if NET20 || NET35 || NET40
    internal sealed class StreamNoDispose : Stream
    {
        private Stream BaseStream { get; }

        public StreamNoDispose(Stream baseStream)
        {
            BaseStream = baseStream;
        }

        public override long Position
        {
            get { return BaseStream.Position; }
            set { BaseStream.Position = value; }
        }

        public override void Flush() => BaseStream.Flush();
        public override void Close() => BaseStream.Flush();
        public override bool CanRead => BaseStream.CanRead;
        public override bool CanSeek => BaseStream.CanSeek;
        public override bool CanWrite => BaseStream.CanWrite;
        public override long Length => BaseStream.Length;
        public override void Write(byte[] buffer, int offset, int count) => BaseStream.Write(buffer, offset, count);
        public override void SetLength(long value) => BaseStream.SetLength(value);
        public override long Seek(long offset, SeekOrigin origin) => BaseStream.Seek(offset, origin);
        public override int Read(byte[] buffer, int offset, int count) => BaseStream.Read(buffer, offset, count);
    }
#endif
}
