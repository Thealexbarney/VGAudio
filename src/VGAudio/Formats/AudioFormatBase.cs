using System;
using System.Collections.Generic;

namespace VGAudio.Formats
{
    public abstract class AudioFormatBase<TFormat, TBuilder> : IAudioFormat
        where TFormat : AudioFormatBase<TFormat, TBuilder>
        where TBuilder : AudioFormatBaseBuilder<TFormat, TBuilder>, new()
    {
        public int SampleCount { get; }
        public int SampleRate { get; }
        public int ChannelCount { get; }
        public int LoopStart { get; }
        public int LoopEnd { get; }
        public bool Looping { get; }

        IAudioFormat IAudioFormat.EncodeFromPcm16(Pcm16Format pcm16) => EncodeFromPcm16(pcm16);
        IAudioFormat IAudioFormat.GetChannels(IEnumerable<int> channelRange) => GetChannels(channelRange);
        IAudioFormat IAudioFormat.WithLoop(bool loop, int loopStart, int loopEnd) => WithLoop(loop, loopStart, loopEnd);
        IAudioFormat IAudioFormat.WithLoop(bool loop) => WithLoop(loop);

        public abstract Pcm16Format ToPcm16();
        public abstract TFormat EncodeFromPcm16(Pcm16Format pcm16);

        protected AudioFormatBase(int sampleCount, int sampleRate, int channelCount)
        {
            SampleCount = sampleCount;
            SampleRate = sampleRate;
            ChannelCount = channelCount;
        }

        protected AudioFormatBase(TBuilder builder)
            : this(builder.SampleCount, builder.SampleRate, builder.ChannelCount)
        {
            Looping = builder.Looping;
            LoopStart = builder.LoopStart;
            LoopEnd = builder.LoopEnd;
        }

        private TFormat GetChannels(IEnumerable<int> channelRange)
        {
            if (channelRange == null)
                throw new ArgumentNullException(nameof(channelRange));

            return GetChannelsInternal(channelRange);
        }

        protected abstract TFormat GetChannelsInternal(IEnumerable<int> channelRange);

        public virtual TFormat WithLoop(bool loop) => GetCloneBuilder().Loop(loop).Build();
        public virtual TFormat WithLoop(bool loop, int loopStart, int loopEnd) =>
            GetCloneBuilder().Loop(loop, loopStart, loopEnd).Build();

        public bool TryAdd(IAudioFormat format)
        {
            TFormat castFormat = format as TFormat;
            if (castFormat == null) return false;
            Add(castFormat);
            return true;
        }

        public virtual TFormat Add(TFormat format)
        {
            if (format.SampleCount != SampleCount)
            {
                throw new ArgumentException("Only audio streams of the same length can be added to each other.");
            }

            return AddInternal(format);
        }

        protected abstract TFormat AddInternal(TFormat format);

        public virtual TBuilder GetCloneBuilder()
        {
            return new TBuilder
            {
                SampleCount = SampleCount,
                SampleRate = SampleRate,
                Looping = Looping,
                LoopStart = LoopStart,
                LoopEnd = LoopEnd
            };
        }
    }
}