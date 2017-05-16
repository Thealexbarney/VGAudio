using System;
using System.IO;
using VGAudio.Formats;

namespace VGAudio.Containers
{
    public abstract class AudioReader<TReader, TStructure, TConfig> : IAudioReader 
        where TReader : AudioReader<TReader, TStructure, TConfig>
        where TConfig : Configuration, new()
    {
        public IAudioFormat ReadFormat(Stream stream) => ReadStream(stream).AudioFormat;
        public IAudioFormat ReadFormat(byte[] file) => ReadByteArray(file).AudioFormat;

        public AudioData Read(Stream stream) => ReadStream(stream).Audio;
        public AudioData Read(byte[] file) => ReadByteArray(file).Audio;

        public AudioWithConfig ReadWithConfig(Stream stream) => ReadStream(stream);
        public AudioWithConfig ReadWithConfig(byte[] file) => ReadByteArray(file);

        public TStructure ReadMetadata(Stream stream) => ReadStructure(stream, false);

        protected virtual TConfig GetConfiguration(TStructure structure) => new TConfig();
        protected abstract TStructure ReadFile(Stream stream, bool readAudioData = true);
        protected abstract IAudioFormat ToAudioStream(TStructure structure);

        private AudioWithConfig ReadByteArray(byte[] file)
        {
            using (var stream = new MemoryStream(file))
            {
                return ReadStream(stream);
            }
        }

        private AudioWithConfig ReadStream(Stream stream)
        {
            TStructure structure = ReadStructure(stream);
            return new AudioWithConfig(ToAudioStream(structure), GetConfiguration(structure));
        }

        private TStructure ReadStructure(Stream stream, bool readAudioData = true)
        {
            if (!stream.CanSeek)
            {
                throw new NotSupportedException("A seekable stream is required");
            }

            return ReadFile(stream, readAudioData);
        }
    }
}
