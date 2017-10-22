using System;
using static VGAudio.Codecs.GcAdpcm.GcAdpcmMath;

namespace VGAudio.Containers.Idsp
{
    /// <summary>
    /// Contains the options used to build the IDSP file.
    /// </summary>
    public class IdspConfiguration : Configuration
    {
        private int _blockSize = BytesPerFrame * 2;

        /// <summary>
        /// If <c>true</c>, recalculates the loop context when building the file.
        /// If <c>false</c>, reuses the loop context read from the imported file,
        /// if available.
        /// Default is <c>true</c>.
        /// </summary>
        public bool RecalculateLoopContext { get; set; } = true;

        /// <summary>
        /// The number of bytes in each block when interleaving the audio data.
        /// Must be divisible by 8.
        /// Default is 16.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if value is negative 
        /// or not divisible by 8.</exception>
        public int BlockSize
        {
            get => _blockSize;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        "Number of samples per interleave must be non-negative");
                }
                if (value % BytesPerFrame != 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        "Number of samples per interleave must be divisible by 14");
                }
                _blockSize = value;
            }
        }
    }
}