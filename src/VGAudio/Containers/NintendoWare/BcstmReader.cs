using System.IO;
using VGAudio.Containers.NintendoWare.Structures;
using VGAudio.Formats;

namespace VGAudio.Containers.NintendoWare
{
    public class BcstmReader : AudioReader<BcstmReader, BxstmStructure, BxstmConfiguration>
    {
        protected override BxstmStructure ReadFile(Stream stream, bool readAudioData = true)
        {
            return new BCFstmReader().ReadFile(stream, readAudioData);
        }

        protected override IAudioFormat ToAudioStream(BxstmStructure structure) => Common.ToAudioStream(structure);

        protected override BxstmConfiguration GetConfiguration(BxstmStructure structure)
        {
            StreamInfo info = structure.StreamInfo;
            var configuration = new BxstmConfiguration();
            if (info.Codec == NwCodec.GcAdpcm)
            {
                configuration.SamplesPerSeekTableEntry = info.SamplesPerSeekTableEntry;
            }
            configuration.Codec = info.Codec;
            configuration.Endianness = structure.Endianness;
            configuration.SamplesPerInterleave = info.SamplesPerInterleave;
            return configuration;
        }
    }
}