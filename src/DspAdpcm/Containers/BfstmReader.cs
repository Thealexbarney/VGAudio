using System.IO;
using DspAdpcm.Containers.Bxstm;
using DspAdpcm.Formats;

namespace DspAdpcm.Containers
{
    public class BfstmReader : AudioReader<BfstmReader, BfstmStructure>
    {
        protected override BfstmStructure ReadFile(Stream stream, bool readAudioData = true)
        {
            return (BfstmStructure)new BCFstmReader().ReadFile(stream, readAudioData);
        }

        protected override IAudioFormat ToAudioStream(BfstmStructure structure)
        {
            return BCFstmReader.ToAudioStream(structure);
        }
    }
}