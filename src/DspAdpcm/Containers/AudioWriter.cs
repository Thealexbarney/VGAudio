using System;
using System.IO;
using DspAdpcm.Formats;

namespace DspAdpcm.Containers
{
    public abstract class AudioWriter<TWriter, TConfig> : IAudioWriter
        where TWriter : AudioWriter<TWriter, TConfig>, new()
        where TConfig : new()
    {
        public byte[] GetFile(IAudioFormat audio) => GetByteArray(new AudioData(audio));
        public void WriteToStream(IAudioFormat audio, Stream stream) => WriteStream(new AudioData(audio), stream);

        public byte[] GetFile(AudioData audio) => GetByteArray(audio);
        public void WriteToStream(AudioData audio, Stream stream) => WriteStream(audio, stream);
        
        protected AudioData AudioStream { get; set; }
        public TConfig Configuration { get; set; } = new TConfig();
        protected abstract int FileSize { get; }

        protected byte[] GetByteArray(AudioData audio)
        {
            SetupWriter(audio);
            var file = new byte[FileSize];
            var stream = new MemoryStream(file);
            WriteStream(audio, stream);
            return file;
        }

        protected void WriteStream(AudioData audio, Stream stream)
        {
            SetupWriter(audio);
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

        protected abstract void SetupWriter(AudioData audio);
        protected abstract void WriteStream(Stream stream);
    }
}
