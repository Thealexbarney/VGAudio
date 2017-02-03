using System;
using DspAdpcm.Codecs;

namespace DspAdpcm
{
    /// <summary>
    /// Represents an audio stream.
    /// The stream can contain any number of individual channels.
    /// </summary>
    public class AudioStream
    {
        private Pcm16Stream _pcm16;
        private AdpcmStream _adpcm;

        /// <summary>
        /// The number of samples in the <see cref="AudioStream"/>.
        /// </summary>
        public int SampleCount { get; }
        /// <summary>
        /// The sample rate of the <see cref="AudioStream"/> in Hz
        /// </summary>
        public int SampleRate { get; set; }

        public int ChannelCount { get; set; }

        /// <summary>
        /// The loop start point in samples.
        /// </summary>
        public int LoopStart { get; internal set; }
        /// <summary>
        /// The loop end point in samples.
        /// </summary>
        public int LoopEnd { get; internal set; }
        /// <summary>
        /// Indicates whether the <see cref="AudioStream"/>
        /// loops or not.
        /// </summary>
        public bool Looping { get; private set; }

        public AudioStream(int sampleCount)
        {
            SampleCount = sampleCount;
        }

        public AudioStream(int sampleCount, int sampleRate)
        {
            SampleCount = sampleCount;
            SampleRate = sampleRate;
        }

        /// <summary>
        /// The 16-bit PCM encoding of the <see cref="AudioStream"/>
        /// </summary>
        public Pcm16Stream Pcm16
        {
            get { return _pcm16 ?? (_pcm16 = new Pcm16Stream(SampleCount)); }
            set { _pcm16 = value; }
        }

        public bool AddPcm16Channel(short[] audio)
        {
            if (audio.Length != SampleCount)
            {
                return false;
            }
            Pcm16.AddChannel(audio);

            ChannelCount = Pcm16.Channels.Count;
            return true;
        }

        public AdpcmStream Adpcm
        {
            get { return _adpcm ?? (_adpcm = new AdpcmStream(SampleCount)); }
            set { _adpcm = value; }
        }

        public bool AddAdpcmChannel(AdpcmChannel audio)
        {
            if (audio.SampleCount != SampleCount)
            {
                return false;
            }
            Adpcm.AddChannel(audio);

            ChannelCount = Adpcm.Channels.Count;
            return true;
        }

        /// <summary>
        /// Sets the loop points for the <see cref="AudioStream"/>.
        /// </summary>
        /// <param name="loopStart">The start loop point in samples.</param>
        /// <param name="loopEnd">The end loop point in samples.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the
        /// specified <paramref name="loopStart"/> or <paramref name="loopEnd"/>
        /// are invalid./></exception>
        public void SetLoop(int loopStart, int loopEnd)
        {
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
    }
}
