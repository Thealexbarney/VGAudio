using System;
using System.Collections.Generic;

namespace VGAudio.Formats
{
    public abstract class AudioFormatBase<T> : IAudioFormat
        where T : AudioFormatBase<T>
    {
        public int SampleCount { get; }
        public int SampleRate { get; }
        public int ChannelCount { get; protected set; }
        public int LoopStart { get; protected set; }
        public int LoopEnd { get; protected set; }
        public bool Looping { get; protected set; }

        IAudioFormat IAudioFormat.EncodeFromPcm16(Pcm16Format pcm16) => EncodeFromPcm16(pcm16);
        IAudioFormat IAudioFormat.GetChannels(IEnumerable<int> channelRange) => GetChannels(channelRange);

        public abstract Pcm16Format ToPcm16();
        public abstract T EncodeFromPcm16(Pcm16Format pcm16);

        public T GetChannels(IEnumerable<int> channelRange)
        {
            if (channelRange == null)
                throw new ArgumentNullException(nameof(channelRange));

            return GetChannelsInternal(channelRange);
        }

        protected abstract T GetChannelsInternal(IEnumerable<int> channelRange);

        protected AudioFormatBase(int sampleCount, int sampleRate, int channelCount)
        {
            SampleCount = sampleCount;
            SampleRate = sampleRate;
            ChannelCount = channelCount;
        }

        public virtual void SetLoop(bool loop, int loopStart, int loopEnd)
        {
            if (!loop)
            {
                SetLoop(false);
                return;
            }

            if (loopStart < 0 || loopStart > SampleCount)
            {
                throw new ArgumentOutOfRangeException(nameof(loopStart), loopStart, "Loop points must be less than the number of samples and non-negative.");
            }

            if (loopEnd < 0 || loopEnd > SampleCount)
            {
                throw new ArgumentOutOfRangeException(nameof(loopEnd), loopEnd, "Loop points must be less than the number of samples and non-negative.");
            }

            if (loopEnd < loopStart)
            {
                throw new ArgumentOutOfRangeException(nameof(loopEnd), loopEnd, "The loop end must be greater than the loop start");
            }

            Looping = true;
            LoopStart = loopStart;
            LoopEnd = loopEnd;
        }

        public virtual void SetLoop(bool loop)
        {
            Looping = loop;
            LoopStart = 0;
            LoopEnd = loop ? SampleCount : 0;
        }

        public bool TryAdd(IAudioFormat format)
        {
            T castFormat = format as T;
            if (castFormat == null) return false;
            Add(castFormat);
            return true;
        }

        public virtual void Add(T format)
        {
            if (format.SampleCount != SampleCount)
            {
                throw new ArgumentException("Only audio streams of the same length can be added to each other.");
            }

            AddInternal(format);
        }

        protected abstract void AddInternal(T format);
    }
}