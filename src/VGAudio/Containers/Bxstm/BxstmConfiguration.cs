using System;
using VGAudio.Utilities;
using static VGAudio.Containers.Bxstm.Common;
using static VGAudio.Formats.GcAdpcm.GcAdpcmHelpers;

namespace VGAudio.Containers.Bxstm
{
    /// <summary>
    /// Contains the options used to build BRSTM, BCSTM and BFSTM files.
    /// </summary>
    public abstract class BxstmConfiguration : Configuration
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
                if (Codec == BxstmCodec.Adpcm && value % SamplesPerFrame != 0)
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

        public BxstmCodec Codec { get; set; } = BxstmCodec.Adpcm;
        public Endianness? Endianness { get; set; }
    }
}