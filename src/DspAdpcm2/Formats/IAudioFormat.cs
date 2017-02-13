namespace DspAdpcm.Formats
{
    public interface IAudioFormat
    {
        int SampleCount { get; }
        int SampleRate { get; }
        int ChannelCount { get; }
        int LoopStart { get; }
        int LoopEnd { get; }
        bool Looping { get; }

        void SetLoop(int loopStart, int loopEnd);
        Pcm16Format ToPcm16();
        IAudioFormat EncodeFromPcm16(Pcm16Format pcm16);
    }
}