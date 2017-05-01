using System;
using static VGAudio.Formats.GcAdpcm.GcAdpcmHelpers;

namespace VGAudio.Containers.Bxstm
{
    /// <summary>
    /// Contains the options used to build BRSTM, BCSTM and BFSTM files.
    /// </summary>
    public abstract class BxstmConfiguration
    {
        private const int Default = -1;
        private int _samplesPerInterleave = Default;
        private int _samplesPerSeekTableEntry = 0x3800;

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
            get
            {
                if (_samplesPerInterleave != Default) return _samplesPerInterleave;

                switch (Codec)
                {
                    case BxstmCodec.Adpcm:
                        return 0x3800;
                    case BxstmCodec.Pcm16Bit:
                        return 0x1000;
                    case BxstmCodec.Pcm8Bit:
                        return 0x2000;
                    default:
                        return 0;
                }
            }
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
            get => _samplesPerSeekTableEntry;
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
        public int LoopPointAlignment { get; set; } = 0x3800;
        public BxstmCodec Codec { get; set; } = BxstmCodec.Adpcm;
    }
}