using System;
using System.Collections.Generic;
using static DspAdpcm.Lib.Helpers;

namespace DspAdpcm.Lib.Adpcm
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
            if (audio.Length != GetBytesForAdpcmSamples(numSamples))
            {
                throw new ArgumentException("Audio array length does not match the specified number of samples.");
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
}
