using System;
using System.Collections.Generic;
using System.Linq;
using VGAudio.Codecs;

namespace VGAudio.Formats
{
    public abstract class AudioFormatBaseBuilder<TFormat, TBuilder, TConfig>
        where TFormat : AudioFormatBase<TFormat, TBuilder, TConfig>
        where TBuilder : AudioFormatBaseBuilder<TFormat, TBuilder, TConfig>
        where TConfig : CodecParameters, new()
    {
        public abstract int ChannelCount { get; }
        protected internal bool Looping { get; set; }
        protected internal int LoopStart { get; set; }
        protected internal int LoopEnd { get; set; }
        protected internal int SampleCount { get; set; }
        protected internal int SampleRate { get; set; }
        protected internal List<AudioTrack> Tracks { get; set; }

        public abstract TFormat Build();

        public virtual TBuilder WithLoop(bool loop, int loopStart, int loopEnd)
        {
            if (!loop)
            {
                return WithLoop(false);
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

        public virtual TBuilder WithLoop(bool loop)
        {
            Looping = loop;
            LoopStart = 0;
            LoopEnd = loop ? SampleCount : 0;
            return this as TBuilder;
        }

        public TBuilder WithTracks(IEnumerable<AudioTrack> tracks)
        {
            Tracks = tracks?.ToList();
            return this as TBuilder;
        }
    }
}
