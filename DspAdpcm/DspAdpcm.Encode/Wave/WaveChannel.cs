using System;
using System.Collections.Generic;
using System.Linq;

namespace DspAdpcm.Encode.Wave
{
    public class WaveChannel : IPcmChannel
    {
        public int GetNumSamples() => Stream.GetNumSamples();
        public int GetSampleRate() => Stream.GetSampleRate();
        public IEnumerable<short> GetAudioChannelData() => Stream.GetAudioData()[CurrentChannel];

        public IPcmStream Stream { get; set; }
        public int CurrentChannel { get; set; }

        public WaveChannel(IPcmStream stream, int channel = 0)
        {
            if (stream.GetAudioData().ElementAtOrDefault(channel) == null)
            {
                throw new ArgumentOutOfRangeException(nameof(channel), $"Channel {channel} does not exist");
            }

            CurrentChannel = channel;
            Stream = stream;
        }
    }
}
