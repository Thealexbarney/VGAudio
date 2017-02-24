using System.IO;
using DspAdpcm.Containers.Bxstm;
using DspAdpcm.Formats;

namespace DspAdpcm.Containers
{
    public class BcstmReader : AudioReader<BcstmReader, BcstmStructure>
    {
        protected override BcstmStructure ReadFile(Stream stream, bool readAudioData = true)
        {
            return (BcstmStructure)new BCFstmReader().ReadFile(stream, readAudioData);
        }

        protected override IAudioFormat ToAudioStream(BcstmStructure structure)
        {
            return BCFstmReader.ToAudioStream(structure);
        }
    }
}