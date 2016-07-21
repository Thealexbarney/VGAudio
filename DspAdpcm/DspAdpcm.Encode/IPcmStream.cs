using System.Collections.Generic;

namespace DspAdpcm.Encode
{
    public interface IPcmStream
    {
        int NumSamples { get; set; }
        int SampleRate { get; set; }
        IList<IPcmChannel> GetChannels();
    }

    public interface IPcmChannel
    {
        int NumSamples { get; set; }
        void AddSample(short sample);
        IEnumerable<short> GetAudioData();
        void SetAudioData(short[] audio);
    }
}
