using System.IO;
using VGAudio.Containers.Bxstm;
using VGAudio.Formats;

namespace VGAudio.Containers
{
    public class BcstmReader : AudioReader<BcstmReader, BcstmStructure, BcstmConfiguration>
    {
        protected override BcstmStructure ReadFile(Stream stream, bool readAudioData = true)
        {
            return (BcstmStructure)new BCFstmReader().ReadFile(stream, readAudioData);
        }

        protected override IAudioFormat ToAudioStream(BcstmStructure structure) => Common.ToAudioStream(structure);

        protected override BcstmConfiguration GetConfiguration(BcstmStructure structure)
        {
            var configuration = new BcstmConfiguration();
            if (structure.Codec == BxstmCodec.Adpcm)
            {
                configuration.SamplesPerSeekTableEntry = structure.SamplesPerSeekTableEntry;
            }
            configuration.Codec = structure.Codec;
            configuration.Endianness = structure.Endianness;
            configuration.SamplesPerInterleave = structure.SamplesPerInterleave;
            configuration.IncludeTrackInformation = structure.IncludeTracks;
            configuration.InfoPart1Extra = structure.InfoPart1Extra;
            return configuration;
        }
    }
}