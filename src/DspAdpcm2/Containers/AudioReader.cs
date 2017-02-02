using System;
using System.IO;

namespace DspAdpcm.Containers
{
    public abstract class AudioReader<TReader, TStructure> : IAudioReader where TReader : AudioReader<TReader, TStructure>, new()
    {
        AudioStream IAudioReader.Read(Stream stream) => ReadStream(stream);
        AudioStream IAudioReader.Read(byte[] file) => ReadByteArray(file);

        public static AudioStream Read(Stream stream) => new TReader().ReadStream(stream);
        public static AudioStream Read(byte[] file) => new TReader().ReadByteArray(file);
        public static TStructure ReadMetadata(Stream stream) => new TReader().GetStructure(stream, false);

        private AudioStream ReadByteArray(byte[] file)
        {
            using (var stream = new MemoryStream(file))
            {
                return ReadStream(stream);
            }
        }

        private AudioStream ReadStream(Stream stream) => ToAudioStream(GetStructure(stream));

        private TStructure GetStructure(Stream stream, bool readAudioData = true)
        {
            if (!stream.CanSeek)
            {
                throw new NotSupportedException("A seekable stream is required");
            }

            return ReadFile(stream, readAudioData);
        }

        protected abstract AudioStream ToAudioStream(TStructure structure);

        protected abstract TStructure ReadFile(Stream stream, bool readAudioData = true);
    }
}
