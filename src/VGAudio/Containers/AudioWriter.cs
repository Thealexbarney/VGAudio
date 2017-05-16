using System;
using System.IO;
using VGAudio.Formats;

namespace VGAudio.Containers
{
    public abstract class AudioWriter<TWriter, TConfig> : IAudioWriter
        where TWriter : AudioWriter<TWriter, TConfig>
        where TConfig : Configuration, new()
    {
        public byte[] GetFile(IAudioFormat audio, Configuration configuration = null) => GetByteArray(new AudioData(audio), configuration as TConfig);
        public void WriteToStream(IAudioFormat audio, Stream stream, Configuration configuration = null) => WriteStream(new AudioData(audio), stream, configuration as TConfig);

        public byte[] GetFile(AudioData audio, Configuration configuration = null) => GetByteArray(audio, configuration as TConfig);
        public void WriteToStream(AudioData audio, Stream stream, Configuration configuration = null) => WriteStream(audio, stream, configuration as TConfig);
        
        protected AudioData AudioStream { get; set; }
        public TConfig Configuration { get; set; } = new TConfig();
        protected abstract int FileSize { get; }

        protected abstract void SetupWriter(AudioData audio);
        protected abstract void WriteStream(Stream stream);

        private byte[] GetByteArray(AudioData audio, TConfig configuration = null)
        {
            Configuration = configuration ?? Configuration;
            SetupWriter(audio);
            var file = new byte[FileSize];
            var stream = new MemoryStream(file);
            WriteStream(stream);
            return file;
        }

        private void WriteStream(AudioData audio, Stream stream, TConfig configuration = null)
        {
            Configuration = configuration ?? Configuration;
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
    }
}
