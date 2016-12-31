using System.Collections.Generic;
using DspAdpcm.Adpcm;
using static DspAdpcm.Helpers;

namespace DspAdpcm {
    /// <summary>
    /// An audio stream that can be used in a BRSTM file.
    /// The only currently supported option is <see cref="AdpcmStream"/>.
    /// </summary>
    public interface BrstmCompatibleStream {
        /// <summary>
        /// The loop start point in samples.
        /// </summary>
        int LoopStart { get; }
        /// <summary>
        /// The loop end point in samples.
        /// </summary>
        int LoopEnd { get; }
        /// <summary>
        /// Indicates whether the <see cref="BrstmCompatibleStream"/>
        /// loops or not.
        /// </summary>
        bool Looping { get; }

        /// <summary>
        /// The number of channels currently in the <see cref="BrstmCompatibleStream"/>.
        /// </summary>
        int NumChannels { get; }
        /// <summary>
        /// The audio sample rate of the <see cref="BrstmCompatibleStream"/>.
        /// </summary>
        int SampleRate { get; }
        /// <summary>
        /// The number of samples in the <see cref="BrstmCompatibleStream"/>.
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
    }
}
