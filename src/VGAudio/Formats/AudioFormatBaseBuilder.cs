using System;

namespace VGAudio.Formats
{
    public abstract class AudioFormatBaseBuilder<TFormat, TBuilder>
        where TFormat : AudioFormatBase<TFormat, TBuilder>
        where TBuilder : AudioFormatBaseBuilder<TFormat, TBuilder>, new()
    {
        internal abstract int ChannelCount { get; }
        internal bool Looping { get; set; }
        internal int LoopStart { get; set; }
        internal int LoopEnd { get; set; }
        internal int SampleCount { get; set; }
        internal int SampleRate { get; set; }

        public abstract TFormat Build();

        public virtual TBuilder Loop(bool loop, int loopStart, int loopEnd)
        {
            if (!loop)
            {
                return Loop(false);
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

            return this as TBuilder;
        }

        public virtual TBuilder Loop(bool loop)
        {
            Looping = loop;
            LoopStart = 0;
            LoopEnd = loop ? SampleCount : 0;
            return this as TBuilder;
        }
    }
}
