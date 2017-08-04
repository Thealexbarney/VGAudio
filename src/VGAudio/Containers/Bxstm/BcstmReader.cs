using System.IO;
using VGAudio.Formats;

namespace VGAudio.Containers.Bxstm
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
            var info = structure.StreamInfo;
            var configuration = new BcstmConfiguration();
            if (info.Codec == BxstmCodec.Adpcm)
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