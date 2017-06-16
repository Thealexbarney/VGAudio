using System.IO;
using VGAudio.Containers.Wave;

namespace VGAudio.Cli.Metadata.Containers
{
    internal class Wave : MetadataReader
    {
        public override Common ToCommon(object structure)
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

        public override object ReadMetadata(Stream stream) => new WaveReader().ReadMetadata(stream);
    }
}
