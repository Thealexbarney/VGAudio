using System.Collections.Generic;
using VGAudio.Utilities.Riff;

namespace VGAudio.Containers.Wave
{
    /// <summary>
    /// Defines the structure and metadata
    /// of a WAVE file
    /// </summary>
    public class WaveStructure
    {
        public List<RiffSubChunk> RiffSubChunks { get; set; }

        /// <summary>The number of channels in the WAVE file.</summary>
        public int ChannelCount { get; set; }

        /// <summary>The audio sample rate.</summary>
        public int SampleRate { get; set; }

        /// <summary>The number of bits per audio sample.</summary>
        public int BitsPerSample { get; set; }

        /// <summary>The number of samples in the audio file.</summary>
        public int SampleCount { get; set; }

        /// <summary>This flag is set if the file loops.</summary>
        public bool Looping { get; set; }

        /// <summary>The loop start position in samples.</summary>
        public int LoopStart { get; set; }

        /// <summary>The loop end position in samples.</summary>
        public int LoopEnd { get; set; }

        public short[][] AudioData16 { get; set; }
        public byte[][] AudioData8 { get; set; }
    }
}
