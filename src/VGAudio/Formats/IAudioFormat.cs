using VGAudio.Codecs;
using VGAudio.Formats.Pcm16;

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

        IAudioFormat WithLoop(bool loop, int loopStart, int loopEnd);
        IAudioFormat WithLoop(bool loop);
        Pcm16Format ToPcm16();
        Pcm16Format ToPcm16(CodecParameters config);
        IAudioFormat EncodeFromPcm16(Pcm16Format pcm16);
        IAudioFormat EncodeFromPcm16(Pcm16Format pcm16, CodecParameters config);
        IAudioFormat GetChannels(params int[] channelRange);
        bool TryAdd(IAudioFormat format, out IAudioFormat result);
    }
}