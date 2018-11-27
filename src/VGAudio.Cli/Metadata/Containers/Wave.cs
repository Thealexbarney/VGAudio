using System.IO;
using VGAudio.Containers.Wave;

namespace VGAudio.Cli.Metadata.Containers
{
    internal class Wave : MetadataReader
    {
        public override Common ToCommon(object structure)
        {
            WaveStructure wave = structure as WaveStructure ?? throw new InvalidDataException("Could not parse file metadata.");

            var format = AudioFormat.None;
            switch (wave.BitsPerSample)
            {
                case 16:
                    format = AudioFormat.Pcm16;
                    break;
                case 8:
                    format = AudioFormat.Pcm8;
                    break;
            }

            return new Common
            {
                SampleCount = wave.SampleCount,
                SampleRate = wave.SampleRate,
                ChannelCount = wave.ChannelCount,
                Looping = wave.Looping,
                LoopStart = wave.LoopStart,
                LoopEnd = wave.LoopEnd,
                Format = format
            };
        }

        public override object ReadMetadata(Stream stream) => new WaveReader().ReadMetadata(stream);
    }
}
