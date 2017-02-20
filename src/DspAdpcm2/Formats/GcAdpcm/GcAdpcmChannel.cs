using System;
using System.Collections.Generic;
using DspAdpcm.Codecs;

namespace DspAdpcm.Formats.GcAdpcm
{
    public class GcAdpcmChannel
    {
        private readonly int _sampleCount;

        public byte[] AudioData { get; }
        public int SampleCount => AlignmentNeeded ? Alignment.SampleCountAligned : _sampleCount;

        public short Gain { get; set; }
        public short[] Coefs { get; set; }
        public short PredScale => AudioData[0];
        public short Hist1 { get; set; }
        public short Hist2 { get; set; }

        public short LoopPredScale(int loopStart, bool ensureSelfCalculated = false) => LoopContext.PredScale(loopStart, ensureSelfCalculated);
        public short LoopHist1(int loopStart, bool ensureSelfCalculated = false) => LoopContext.Hist1(loopStart, ensureSelfCalculated);
        public short LoopHist2(int loopStart, bool ensureSelfCalculated = false) => LoopContext.Hist2(loopStart, ensureSelfCalculated);

        public List<GcAdpcmSeekTable> SeekTable { get; } = new List<GcAdpcmSeekTable>();
        private GcAdpcmLoopContext LoopContext { get; }
        private GcAdpcmAlignment Alignment { get; } = new GcAdpcmAlignment();
        private bool AlignmentNeeded { get; set; }

        public GcAdpcmChannel(int sampleCount)
        {
            _sampleCount = sampleCount;
            AudioData = new byte[GcAdpcmHelpers.SampleCountToByteCount(sampleCount)];
            LoopContext = new GcAdpcmLoopContext(this);
        }

        public GcAdpcmChannel(int sampleCount, byte[] audio)
        {
            if (audio.Length < GcAdpcmHelpers.SampleCountToByteCount(sampleCount))
            {
                throw new ArgumentException("Audio array length is too short for the specified number of samples.");
            }

            _sampleCount = sampleCount;
            AudioData = audio;
            LoopContext = new GcAdpcmLoopContext(this);
        }

        public short[] GetPcmAudioLooped(int startSample, int length, int loopStart, int loopEnd,
            bool includeHistorySamples = false)
        {
            if (startSample + length <= loopEnd)
            {
                return GcAdpcmDecoder.Decode(this, startSample, length, includeHistorySamples);
            }

            short[] pcm = GcAdpcmDecoder.Decode(this, 0, loopEnd, includeHistorySamples);

            if (includeHistorySamples)
            {
                length += 2;
                loopStart += 2;
                loopEnd += 2;
            }

            short[] output = new short[length];

            int outIndex = 0;
            int samplesRemaining = length;
            int currentSample = GetLoopedSample(startSample, loopStart, loopEnd);

            while (samplesRemaining > 0)
            {
                int samplesToGet = Math.Min(loopEnd - currentSample, samplesRemaining);
                Array.Copy(pcm, currentSample, output, outIndex, samplesToGet);
                samplesRemaining -= samplesToGet;
                outIndex += samplesToGet;
                currentSample = loopStart;
            }

            return output;
        }

        internal void SetAlignment(int multiple, int loopStart, int loopEnd)
        {
            AlignmentNeeded = Alignment.SetAlignment(multiple, loopStart, loopEnd, this);
        }

        private static int GetLoopedSample(int sample, int loopStart, int loopEnd)
        {
            return sample < loopStart ? sample : (sample - loopStart) % (loopEnd - loopStart + 1) + loopStart;
        }

        public byte[] GetAudioData()
        {
            return AlignmentNeeded ? Alignment.AudioDataAligned : AudioData;
        }

        public void SetLoopContext(int loopStart, short predScale, short hist1, short hist2)
            => LoopContext.AddLoopContext(loopStart, predScale, hist1, hist2);
    }
}
