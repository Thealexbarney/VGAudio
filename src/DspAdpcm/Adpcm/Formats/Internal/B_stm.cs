using System;
using static DspAdpcm.Helpers;

namespace DspAdpcm.Adpcm.Formats.Internal
{
    /// <summary>
    /// Contains the options used to build audio files.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public interface B_stmConfiguration
    {
        /// <summary>
        /// The number of samples in each block when interleaving
        /// the audio data in the audio file.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if value is negative 
        /// or not appropriate for the chosen encoding.</exception>
        int SamplesPerInterleave { get; set; }
        
        /// <summary>
        /// When building the audio file, the loop points and audio will
        /// be adjusted so that the start loop point is a multiple of
        /// this number.
        /// </summary>
        int LoopPointAlignment { get; set; }
    }

    /// <summary>
    /// Contains the options used to build audio files, including options
    /// specific to ADPCM encoding.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public abstract class AdpcmB_stmConfiguration : B_stmConfiguration
    {
        internal AdpcmB_stmConfiguration() { }
        private int _samplesPerInterleave = 0x3800;
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
            get { return _samplesPerInterleave; }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        "Number of samples per interleave must be positive");
                }
                if (value % SamplesPerFrame != 0)
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
            get { return _samplesPerSeekTableEntry; }
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
    }
    
    /// <summary>
    /// Contains the options used to build audio files, including options
    /// specific to PCM encoding.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public abstract class PcmB_stmConfiguration : B_stmConfiguration
    {
        internal PcmB_stmConfiguration() { }
        private int _samplesPerInterleave = 0x1000;

        /// <summary>
        /// The number of samples in each block when interleaving
        /// the audio data in the audio file.
        /// Default is 4,096 (0x1000).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if value is negative.</exception>
        public int SamplesPerInterleave
        {
            get { return _samplesPerInterleave; }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        "Number of samples per interleave must be positive");
                }
                _samplesPerInterleave = value;
            }
        }

        /// <summary>
        /// When building the audio file, the loop points and audio will
        /// be adjusted so that the start loop point is a multiple of
        /// this number. Default is 4,096 (0x1000).
        /// </summary>
        public int LoopPointAlignment { get; set; } = 0x1000;
    }
}
