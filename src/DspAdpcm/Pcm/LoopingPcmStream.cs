using System;
using System.Collections.Generic;
using static DspAdpcm.Helpers;
using DspAdpcm.Adpcm;

#if NET20
using DspAdpcm.Compatibility.LinqBridge;
#else
using System.Linq;
#endif

namespace DspAdpcm.Pcm
{
    /// <summary>
    /// A 16-bit PCM audio stream with one or more tracks optional looping data.
    /// Each track can contain one or two channels.
    /// </summary>
    public class LoopingPcmStream : PcmStream, LoopingTrackStream
    {
        /// <summary>
        /// The loop start point in samples.
        /// </summary>
        public int LoopStart { get; internal set; }
        /// <summary>
        /// The loop end point in samples.
        /// </summary>
        public int LoopEnd { get; internal set; }
        /// <summary>
        /// Indicates whether the <see cref="LoopingPcmStream"/>
        /// loops or not.
        /// </summary>
        public bool Looping { get; private set; }

        private List<AdpcmTrack> _tracks;

        /// <summary>
        /// A list of tracks in the stream. Each track is composed of one or two channels.
        /// </summary>
        public List<AdpcmTrack> Tracks {
            get { return _tracks == null || _tracks.Count == 0 ? GetDefaultTrackList().ToList() : _tracks; }
            internal set { _tracks = value; }
        }

        /// <summary>
        /// Creates an empty <see cref="LoopingPcmStream"/> and sets the
        /// number of samples and sample rate.
        /// </summary>
        /// <param name="numSamples">The sample count.</param>
        /// <param name="sampleRate">The sample rate in Hz.</param>
        public LoopingPcmStream(int numSamples, int sampleRate) : base(numSamples, sampleRate) { }

        /// <summary>
        /// Sets the loop points for the <see cref="LoopingPcmStream"/>.
        /// </summary>
        /// <param name="loopStart">The start loop point in samples.</param>
        /// <param name="loopEnd">The end loop point in samples.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the
        /// specified <paramref name="loopStart"/> or <paramref name="loopEnd"/>
        /// are invalid./></exception>
        public void SetLoop(int loopStart, int loopEnd)
        {
            if (loopStart < 0 || loopStart > NumSamples)
            {
                throw new ArgumentOutOfRangeException(nameof(loopStart), loopStart, "Loop points must be less than the number of samples and non-negative.");
            }

            if (loopEnd < 0 || loopEnd > NumSamples)
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

        /// <summary>
        /// Sets the loop points for the <see cref="LoopingPcmStream"/>.
        /// </summary>
        /// <param name="loop">If <c>false</c>, don't loop the <see cref="LoopingPcmStream"/>.
        /// If <c>true</c>, loop the <see cref="LoopingPcmStream"/> from 0 to <see cref="PcmStream.NumSamples"/></param>
        public void SetLoop(bool loop)
        {
            Looping = loop;
            LoopStart = 0;
            LoopEnd = loop ? NumSamples : 0;
        }

        /// <summary>
        /// Get an array of byte arrays containing the encoded audio data (one array per channel.)
        /// </summary>
        /// <param name="endianness">The endianness of the format (not used for ADPCM)</param>
        /// <returns>An array that contains one byte array per channel</returns>
        public byte[][] GetAudioData(Endianness endianness)
        {
            return Channels.Select(x => x.GetAudioData(endianness)).ToArray();
        }

        private IEnumerable<AdpcmTrack> GetDefaultTrackList()
        {
            int numTracks = Channels.Count.DivideByRoundUp(2);
            for (int i = 0; i < numTracks; i++)
            {
                int numChannels = Math.Min(Channels.Count - i * 2, 2);
                yield return new AdpcmTrack
                {
                    NumChannels = numChannels,
                    ChannelLeft = i * 2,
                    ChannelRight = numChannels >= 2 ? i * 2 + 1 : 0
                };
            }
        }
    }
}
