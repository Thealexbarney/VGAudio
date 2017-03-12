using System.IO;
using VGAudio.Containers;
using VGAudio.Containers.Idsp;

namespace VGAudio.Cli.Metadata.Containers
{
    internal class Idsp : MetadataReader
    {
        public override Common ToCommon(object structure)
        {
            var idsp = structure as IdspStructure;
            if (idsp == null) throw new InvalidDataException("Could not parse file metadata.");

            return new Common
            {
                SampleCount = idsp.SampleCount,
                SampleRate = idsp.SampleRate,
                ChannelCount = idsp.ChannelCount,
                Format = AudioFormat.GcAdpcm,
                Looping = idsp.Looping,
                LoopStart = idsp.LoopStart,
                LoopEnd = idsp.LoopEnd
            };
        }

        public override object ReadMetadata(Stream stream) => new IdspReader().ReadMetadata(stream);
    }
}
