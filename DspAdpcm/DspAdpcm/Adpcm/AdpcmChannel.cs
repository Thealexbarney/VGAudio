using System;
using static DspAdpcm.Helpers;

namespace DspAdpcm.Adpcm
{
    internal class AdpcmChannel
    {
        private readonly int _numSamples;
        public byte[] AudioByteArray { get; set; }

        public int NumSamples => AudioByteArrayAligned == null ? _numSamples : NumSamplesAligned;

        public short Gain { get; set; }
        public short[] Coefs { get; set; }
        public short Hist1 { get; set; }
        public short Hist2 { get; set; }

        public short LoopPredScale { get; private set; }
        public short LoopHist1 { get; private set; }
        public short LoopHist2 { get; private set; }

        public short[] SeekTable { get; set; }
        public int SamplesPerSeekTableEntry { get; set; }
        public bool LoopContextCalculated { get; private set; }
        public bool SelfCalculatedSeekTable { get; set; }
        public bool SelfCalculatedLoopContext { get; set; }

        public byte[] AudioByteArrayAligned { get; set; }
        public int LoopAlignment { get; set; }
        public int LoopStartAligned { get; set; }
        public int LoopEndAligned { get; set; }
        public int NumSamplesAligned { get; set; }

        public AdpcmChannel(int numSamples)
        {
            AudioByteArray = new byte[GetBytesForAdpcmSamples(numSamples)];
            _numSamples = numSamples;
        }

        public AdpcmChannel(int numSamples, byte[] audio)
        {
            if (audio.Length < GetBytesForAdpcmSamples(numSamples))
            {
                throw new ArgumentException("Audio array length is too short for the specified number of samples.");
            }
            AudioByteArray = audio;
            _numSamples = numSamples;
        }

        public AdpcmChannel SetLoopContext(short loopPredScale, short loopHist1, short loopHist2)
        {
            LoopPredScale = loopPredScale;
            LoopHist1 = loopHist1;
            LoopHist2 = loopHist2;

            LoopContextCalculated = true;
            return this;
        }

        public byte[] GetAudioData => AudioByteArrayAligned ?? AudioByteArray;
    }

    /// <summary>
    /// Defines the ADPCM information for a single
    /// ADPCM channel.
    /// </summary>
    public class AdpcmChannelInfo
    {
        /// <summary>
        /// The ADPCM coefficients of the channel.
        /// </summary>
        public short[] Coefs { get; set; }

        /// <summary>
        /// The gain level for the channel.
        /// </summary>
        public short Gain { get; set; }
        /// <summary>
        /// The predictor and scale for the first
        /// frame of the channel.
        /// </summary>
        public short PredScale { get; set; }
        /// <summary>
        /// The first PCM history sample for the stream.
        /// (Initial sample - 1).
        /// </summary>
        public short Hist1 { get; set; }
        /// <summary>
        /// The second PCM history sample for the stream.
        /// (Initial sample - 2).
        /// </summary>
        public short Hist2 { get; set; }

        /// <summary>
        /// The predictor and scale for the loop
        /// point frame.
        /// </summary>
        public short LoopPredScale { get; set; }
        /// <summary>
        /// The first PCM history sample for the start
        /// loop point. (loop point - 1).
        /// </summary>
        public short LoopHist1 { get; set; }
        /// <summary>
        /// The second PCM history sample for the start
        /// loop point. (loop point - 2).
        /// </summary>
        public short LoopHist2 { get; set; }
    }
}
