using System;
using System.IO;
using DspAdpcm.Formats;

namespace DspAdpcm.Containers
{
    public abstract class AudioReader<TReader, TStructure> : IAudioReader 
        where TReader : AudioReader<TReader, TStructure>, new()
    {
        IAudioFormat IAudioReader.ReadFormat(Stream stream) => ReadStream(stream);
        IAudioFormat IAudioReader.ReadFormat(byte[] file) => ReadByteArray(file);
        AudioData IAudioReader.Read(Stream stream) => new AudioData(ReadStream(stream));
        AudioData IAudioReader.Read(byte[] file) => new AudioData(ReadByteArray(file));

        public static IAudioFormat ReadFormat(Stream stream) => new TReader().ReadStream(stream);
        public static IAudioFormat ReadFormat(byte[] file) => new TReader().ReadByteArray(file);
        public static AudioData Read(Stream stream) => new AudioData(ReadFormat(stream));
        public static AudioData Read(byte[] file) => new AudioData(ReadFormat(file));

        public static TStructure ReadMetadata(Stream stream) => new TReader().GetStructure(stream, false);

        private IAudioFormat ReadByteArray(byte[] file)
        {
            using (var stream = new MemoryStream(file))
            {
                return ReadStream(stream);
            }
        }

        private IAudioFormat ReadStream(Stream stream) => ToAudioStream(GetStructure(stream));

        private TStructure GetStructure(Stream stream, bool readAudioData = true)
        {
            if (!stream.CanSeek)
            {
                throw new NotSupportedException("A seekable stream is required");
            }

            return ReadFile(stream, readAudioData);
        }

        protected abstract TStructure ReadFile(Stream stream, bool readAudioData = true);
        protected abstract IAudioFormat ToAudioStream(TStructure structure);
    }
}
