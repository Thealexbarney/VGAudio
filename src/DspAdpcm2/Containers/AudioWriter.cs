using System;
using System.IO;

namespace DspAdpcm.Containers
{
    public abstract class AudioWriter<TWriter> : IAudioWriter where TWriter : AudioWriter<TWriter>, new()
    {
        byte[] IAudioWriter.GetFile(AudioStream audio) => GetByteArray(audio);
        void IAudioWriter.WriteToStream(AudioStream audio, Stream stream) => WriteStream(audio, stream);

        public static byte[] GetFile(AudioStream audio) => new TWriter().GetByteArray(audio);
        public static void WriteToStream(AudioStream audio, Stream stream)
            => new TWriter().WriteStream(audio, stream);

        protected AudioStream AudioStream { get; set; }
        protected abstract int FileSize { get; }

        private byte[] GetByteArray(AudioStream audio)
        {
            AudioStream = audio;
            var file = new byte[FileSize];
            var stream = new MemoryStream(file);
            WriteStream(audio, stream);
            return file;
        }

        private void WriteStream(AudioStream audio, Stream stream)
        {
            AudioStream = audio;
            if (stream.Length == FileSize) return;
            try
            {
                stream.SetLength(FileSize);
            }
            catch (NotSupportedException ex)
            {
                throw new ArgumentException("Stream is too small.", nameof(stream), ex);
            }

            WriteStream(stream);
        }

        protected abstract void WriteStream(Stream stream);
    }
}
