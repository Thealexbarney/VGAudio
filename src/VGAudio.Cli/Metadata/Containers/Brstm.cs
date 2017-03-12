using System.IO;
using VGAudio.Containers;
using VGAudio.Containers.Bxstm;

namespace VGAudio.Cli.Metadata.Containers
{
    internal class Brstm : IMetadataReader
    {
        public Common ToCommon(object structure)
        {
            var brstm = structure as BrstmStructure;
            if (brstm == null) throw new InvalidDataException("Could not parse file metadata.");

            return new Common
            {
                SampleCount = brstm.SampleCount,
                SampleRate = brstm.SampleRate,
                ChannelCount = brstm.ChannelCount,
                Format = AudioFormat.GcAdpcm,
                Looping = brstm.Looping,
                LoopStart = brstm.LoopStart,
                LoopEnd = brstm.SampleCount
            };
        }

        public object ReadMetadata(Stream stream) => new BrstmReader().ReadMetadata(stream);
    }
}
