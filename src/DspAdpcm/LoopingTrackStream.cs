using System;
using System.Collections.Generic;
using DspAdpcm.Adpcm;
using static DspAdpcm.Helpers;

namespace DspAdpcm {
    /// <summary>
    /// An audio stream that can be used in a BRSTM file.
    /// The only currently supported option is <see cref="AdpcmStream"/>.
    /// </summary>
    public interface LoopingTrackStream {
        /// <summary>
        /// The loop start point in samples.
        /// </summary>
        int LoopStart { get; }
        /// <summary>
        /// The loop end point in samples.
        /// </summary>
        int LoopEnd { get; }
        /// <summary>
        /// Indicates whether the <see cref="LoopingTrackStream"/>
        /// loops or not.
        /// </summary>
        bool Looping { get; }

        /// <summary>
        /// The number of channels currently in the <see cref="LoopingTrackStream"/>.
        /// </summary>
        int NumChannels { get; }
        /// <summary>
        /// The audio sample rate of the <see cref="LoopingTrackStream"/>.
        /// </summary>
        int SampleRate { get; }
        /// <summary>
        /// The number of samples in the <see cref="LoopingTrackStream"/>.
        /// </summary>
        int NumSamples { get; }

        /// <summary>
        /// A list of tracks in the stream. Each track is composed of one or two channels.
        /// </summary>
        List<AdpcmTrack> Tracks { get; }

        /// <summary>
        /// Get an array of byte arrays containing the encoded audio data (one array per channel.)
        /// </summary>
        /// <param name="endianness">The endianness of the format (only used for 16-bit PCM)</param>
        /// <returns>An array that contains one byte array per channel</returns>
        byte[][] GetAudioData(Endianness endianness);

        /// <summary>
        /// Sets the loop points for the <see cref="LoopingTrackStream"/>.
        /// </summary>
        /// <param name="loopStart">The start loop point in samples.</param>
        /// <param name="loopEnd">The end loop point in samples.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the
        /// specified <paramref name="loopStart"/> or <paramref name="loopEnd"/>
        /// are invalid./></exception>
        void SetLoop(int loopStart, int loopEnd);

        /// <summary>
        /// Sets the loop points for the <see cref="LoopingTrackStream"/>.
        /// </summary>
        /// <param name="loop">If <c>false</c>, don't loop the <see cref="LoopingTrackStream"/>.
        /// If <c>true</c>, loop the <see cref="LoopingTrackStream"/> from 0 to <see cref="NumSamples"/></param>
        void SetLoop(bool loop);
    }
}
