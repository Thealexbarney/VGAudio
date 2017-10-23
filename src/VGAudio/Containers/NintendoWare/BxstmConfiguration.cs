using System;
using VGAudio.Containers.NintendoWare.Structures;
using VGAudio.Utilities;
using static VGAudio.Containers.NintendoWare.Common;
using static VGAudio.Codecs.GcAdpcm.GcAdpcmMath;

namespace VGAudio.Containers.NintendoWare
{
    /// <summary>
    /// Contains the options used to build BRSTM, BCSTM and BFSTM files.
    /// </summary>
    public class BxstmConfiguration : Configuration
    {
        private const int Default = -1;
        private const int DefaultInterleave = 0x2000;
        private int _loopPointAlignment = Default;
        private int _samplesPerInterleave = Default;
        private int _samplesPerSeekTableEntry = Default;

        /// <summary>
        /// If <c>true</c>, rebuilds the seek table when building the file.
        /// If <c>false</c>, reuses the seek table read from the imported file,
        /// if available.
        /// Default is <c>true</c>.
        /// </summary>
        public bool RecalculateSeekTable { get; set; } = true;

        /// <summary>
        /// If <c>true</c>, recalculates the loop context when building the file.
        /// If <c>false</c>, reuses the loop context read from the imported file,
        /// if available.
        /// Default is <c>true</c>.
        /// </summary>
        public bool RecalculateLoopContext { get; set; } = true;

        /// <summary>
        /// The number of samples in each block when interleaving
        /// the audio data in the audio file.
        /// Must be divisible by 14.
        /// Default is 14,336 (0x3800).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if value is negative 
        /// or not divisible by 14.</exception>
        public int SamplesPerInterleave
        {
            get => _samplesPerInterleave != Default ? _samplesPerInterleave : BytesToSamples(DefaultInterleave, Codec);
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        "Number of samples per interleave must be positive");
                }
                if (Codec == NwCodec.GcAdpcm && value % SamplesPerFrame != 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        "Number of samples per interleave must be divisible by 14");
                }
                _samplesPerInterleave = value;
            }
        }

        /// <summary>
        /// The number of samples per entry in the seek table. Used when
        /// building the audio file.
        /// Default is 14,336 (0x3800).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if
        /// value is less than 2.</exception>
        public int SamplesPerSeekTableEntry
        {
            get => _samplesPerSeekTableEntry != Default ? _samplesPerSeekTableEntry : BytesToSamples(DefaultInterleave, Codec);
            set
            {
                if (value < 2)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        "Number of samples per interleave must be 2 or greater");
                }
                _samplesPerSeekTableEntry = value;
            }
        }

        /// <summary>
        /// When building the audio file, the loop points and audio will
        /// be adjusted so that the start loop point is a multiple of
        /// this number. Default is 14,336 (0x3800).
        /// </summary>
        public int LoopPointAlignment
        {
            get => _loopPointAlignment != Default ? _loopPointAlignment : BytesToSamples(DefaultInterleave, Codec);
            set => _loopPointAlignment = value;
        }

        public NwCodec Codec { get; set; } = NwCodec.GcAdpcm;
        public Endianness? Endianness { get; set; }
        public NwVersion Version { get; set; }

        /// <summary>
        /// The type of track description to be used when building the 
        /// BRSTM header.
        /// Default is <see cref="BrstmTrackType.Standard"/>.
        /// Used only in BRSTM files.
        /// </summary>
        public BrstmTrackType TrackType { get; set; } = BrstmTrackType.Standard;

        /// <summary>
        /// The type of seek table to use when building the BRSTM
        /// ADPC block.
        /// Default is <see cref="BrstmSeekTableType.Standard"/>.
        /// Used only in BRSTM files.
        /// </summary>
        public BrstmSeekTableType SeekTableType { get; set; } = BrstmSeekTableType.Standard;
    }
}