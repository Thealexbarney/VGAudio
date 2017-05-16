using System.IO;
using VGAudio.Containers.Bxstm;
using VGAudio.Formats;

namespace VGAudio.Containers
{
    public class BfstmReader : AudioReader<BfstmReader, BfstmStructure, BfstmConfiguration>
    {
        protected override BfstmStructure ReadFile(Stream stream, bool readAudioData = true)
        {
            return (BfstmStructure)new BCFstmReader().ReadFile(stream, readAudioData);
        }

        protected override IAudioFormat ToAudioStream(BfstmStructure structure) => Common.ToAudioStream(structure);

        protected override BfstmConfiguration GetConfiguration(BfstmStructure structure)
        {
            var configuration = new BfstmConfiguration();
            if (structure.Codec == BxstmCodec.Adpcm)
            {
                configuration.SamplesPerSeekTableEntry = structure.SamplesPerSeekTableEntry;
            }
            configuration.Codec = structure.Codec;
            configuration.Endianness = structure.Endianness;
            configuration.SamplesPerInterleave = structure.SamplesPerInterleave;
            configuration.IncludeUnalignedLoopPoints = structure.Version == 4;
            return configuration;
        }
    }
}