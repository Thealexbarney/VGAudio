using System.IO;
using VGAudio.Containers;
using VGAudio.Containers.Wave;

namespace VGAudio.Cli.Metadata.Containers
{
    internal class Wave : IMetadataReader
    {
        public Common ToCommon(object structure)
        {
            var wave = structure as WaveStructure;
            if (wave == null) throw new InvalidDataException("Could not parse file metadata.");

            return new Common
            {
                SampleCount = wave.SampleCount,
                SampleRate = wave.SampleRate,
                ChannelCount = wave.ChannelCount,
                Format = AudioFormat.Pcm16,
                Looping = false
            };
        }

        public object ReadMetadata(Stream stream) => new WaveReader().ReadMetadata(stream);
    }
}
