using System;
using System.Collections.Generic;

namespace DspAdpcm.Adpcm.Formats.Structures
{
    /// <summary>
    /// Defines the structure of a GENH file.
    /// </summary>
    public class GenhStructure
    {
        /// <summary>
        /// The number of channels in the file.
        /// </summary>
        public int NumChannels { get; set; }
        /// <summary>
        /// The number of bytes per channel in each
        /// interleaved audio data block.
        /// </summary>
        public int Interleave { get; set; }
        /// <summary>
        /// The sample rate of the audio.
        /// </summary>
        public int SampleRate { get; set; }
        /// <summary>
        /// The start loop point in samples.
        /// Will be <c>-1</c> if audio doesn't loop.
        /// </summary>
        public int LoopStart { get; set; }
        /// <summary>
        /// The end loop point in samples.
        /// </summary>
        public int LoopEnd { get; set; }
        /// <summary>
        /// The audio codec of the audio data contained in the GENH.
        /// </summary>
        public int Codec { get; set; }
        /// <summary>
        /// The offset that the actual audio data starts at.
        /// </summary>
        public int AudioDataOffset { get; set; }
        /// <summary>
        /// The size of the GENH header.
        /// </summary>
        public int HeaderSize { get; set; }
        /// <summary>
        /// The offsets of the ADPCM coefficients for the 
        /// left and right channels, respectively.
        /// </summary>
        public int[] Coefs { get; set; } = new int[2];
        /// <summary>
        /// The offsets of the split ADPCM coefficients for the 
        /// left and right channels, respectively.
        /// </summary>
        public int[] CoefsSplit { get; set; } = new int[2];
        /// <summary>
        /// The interleave type of the GENH file.
        /// </summary>
        public int InterleaveType { get; set; }
        /// <summary>
        /// Specifies how the ADPCM coefficients are stored.
        /// </summary>
        public GenhCoefType CoefType { get; set; }
        /// <summary>
        /// The ADPCM information for each channel.
        /// </summary>
        public IList<AdpcmChannelInfo> Channels { get; } = new List<AdpcmChannelInfo>();

        /// <summary>
        /// The number of samples in the file.
        /// </summary>
        public int NumSamples => LoopEnd;
        /// <summary>
        /// This flag is set if the file loops.
        /// </summary>
        public bool Looping => LoopStart != -1;
        internal byte[][] AudioData { get; set; }
    }

    /// <summary>
    /// Flags for how the ADPCM coefficients are stored in a GENH file.
    /// </summary>
    [Flags]
    public enum GenhCoefType
    {
        /// <summary>
        /// Set if the ADPCM coefficients are in column-major order.
        /// </summary>
        Split = 1,
        /// <summary>
        /// Set if the ADPCM coefficients are little endian.
        /// </summary>
        LittleEndian = 2
    }
}
