using System.Collections.Generic;

namespace DspAdpcm.Encode
{
    public interface IAudioStream
    {
        int GetNumSamples();
        int GetSampleRate();
        IEnumerable<short> GetAudioData();
    }
}
