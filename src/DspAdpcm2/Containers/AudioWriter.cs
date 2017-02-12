using System;
using System.IO;
using DspAdpcm.Formats;

namespace DspAdpcm.Containers
{
    public abstract class AudioWriter<TWriter> : IAudioWriter
        where TWriter : AudioWriter<TWriter>, new()
    {
        byte[] IAudioWriter.GetFile(IAudioFormat audio) => GetByteArray(new AudioData(audio));
        void IAudioWriter.WriteToStream(IAudioFormat audio, Stream stream) => WriteStream(new AudioData(audio), stream);

        byte[] IAudioWriter.GetFile(AudioData audio) => GetByteArray(audio);
        void IAudioWriter.WriteToStream(AudioData audio, Stream stream) => WriteStream(audio, stream);

        public static byte[] GetFile(IAudioFormat audio) => new TWriter().GetByteArray(new AudioData(audio));
        public static void WriteToStream(IAudioFormat audio, Stream stream)
            => new TWriter().WriteStream(new AudioData(audio), stream);

        public static byte[] GetFile(AudioData audio) => new TWriter().GetByteArray(audio);
        public static void WriteToStream(AudioData audio, Stream stream)
            => new TWriter().WriteStream(audio, stream);

        protected AudioData AudioStream { get; set; }
        protected abstract int FileSize { get; }

        protected byte[] GetByteArray(AudioData audio)
        {
            SetAudioFormat(audio);
            var file = new byte[FileSize];
            var stream = new MemoryStream(file);
            WriteStream(audio, stream);
            return file;
        }

        protected void WriteStream(AudioData audio, Stream stream)
        {
            SetAudioFormat(audio);
            if (stream.Length != FileSize)
            {
                try
                {
                    stream.SetLength(FileSize);
                }
                catch (NotSupportedException ex)
                {
                    throw new ArgumentException("Stream is too small.", nameof(stream), ex);
                }
            }

            WriteStream(stream);
        }

        protected abstract void SetAudioFormat(AudioData audio);
        protected abstract void WriteStream(Stream stream);
    }
}
