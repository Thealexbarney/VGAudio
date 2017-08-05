using System.IO;
using VGAudio.Formats;

namespace VGAudio.Containers.NintendoWare
{
    public class BfstmReader : AudioReader<BfstmReader, BxstmStructure, BxstmConfiguration>
    {
        protected override BxstmStructure ReadFile(Stream stream, bool readAudioData = true)
        {
            return new BCFstmReader().ReadFile(stream, readAudioData);
        }

        protected override IAudioFormat ToAudioStream(BxstmStructure structure) => Common.ToAudioStream(structure);

        protected override BxstmConfiguration GetConfiguration(BxstmStructure structure)
        {
            var configuration = new BxstmConfiguration();
            if (structure.StreamInfo.Codec == NwCodec.GcAdpcm)
            {
                configuration.SamplesPerSeekTableEntry = structure.StreamInfo.SamplesPerSeekTableEntry;
            }
            configuration.Codec = structure.StreamInfo.Codec;
            configuration.Endianness = structure.Endianness;
            configuration.SamplesPerInterleave = structure.StreamInfo.SamplesPerInterleave;
            return configuration;
        }
    }
}