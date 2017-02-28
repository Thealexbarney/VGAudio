using System.Collections.Generic;

namespace VGAudio.Formats
{
    public interface IAudioFormat
    {
        int SampleCount { get; }
        int SampleRate { get; }
        int ChannelCount { get; }
        int LoopStart { get; }
        int LoopEnd { get; }
        bool Looping { get; }

        void SetLoop(bool loop, int loopStart, int loopEnd);
        void SetLoop(bool loop);
        Pcm16Format ToPcm16();
        IAudioFormat EncodeFromPcm16(Pcm16Format pcm16);
        IAudioFormat GetChannels(IEnumerable<int> channelRange);
        bool TryAdd(IAudioFormat format);
    }
}