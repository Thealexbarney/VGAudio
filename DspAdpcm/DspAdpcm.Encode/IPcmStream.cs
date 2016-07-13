using System.Collections.Generic;

namespace DspAdpcm.Encode
{
    public interface IPcmStream
    {
        int GetNumSamples();
        int GetSampleRate();
        IList<IEnumerable<short>> GetAudioData();
        IList<IPcmChannel> GetChannels();
    }

    public interface IPcmChannel
    {
        int GetNumSamples();
        int GetSampleRate();
        IEnumerable<short> GetAudioChannelData();
    }
}
