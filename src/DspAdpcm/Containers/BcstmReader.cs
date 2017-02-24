using System.IO;
using DspAdpcm.Containers.Bxstm;
using DspAdpcm.Formats;

namespace DspAdpcm.Containers
{
    public class BcstmReader : AudioReader<BcstmReader, BcstmStructure, BcstmConfiguration>
    {
        protected override BcstmStructure ReadFile(Stream stream, bool readAudioData = true)
        {
            return (BcstmStructure)new BCFstmReader().ReadFile(stream, readAudioData);
        }

        protected override IAudioFormat ToAudioStream(BcstmStructure structure)
        {
            return BCFstmReader.ToAudioStream(structure);
        }

        protected override BcstmConfiguration GetConfiguration(BcstmStructure structure)
        {
            return new BcstmConfiguration
            {
                SamplesPerInterleave = structure.SamplesPerInterleave,
                SamplesPerSeekTableEntry = structure.SamplesPerSeekTableEntry,
                IncludeTrackInformation = structure.IncludeTracks,
                InfoPart1Extra = structure.InfoPart1Extra,
            };
        }
    }
}