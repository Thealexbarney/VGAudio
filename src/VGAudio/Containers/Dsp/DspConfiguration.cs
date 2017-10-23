using System;
using static VGAudio.Codecs.GcAdpcm.GcAdpcmMath;

namespace VGAudio.Containers.Dsp
{
    /// <summary>
    /// Contains the options used to build the DSP file.
    /// </summary>
    public class DspConfiguration : Configuration
    {
        private int _samplesPerInterleave = 0x3800;
        /// <summary>
        /// If <c>true</c>, recalculates the loop context when building the DSP.
        /// If <c>false</c>, reuses the loop context read from an imported DSP
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
            get => _samplesPerInterleave;
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
        /// When building the DSP file, the loop points and audio will
        /// be adjusted so that the start loop point is a multiple of
        /// this number. Default is 1.
        /// </summary>
        public int LoopPointAlignment { get; set; } = 1;
    }
}