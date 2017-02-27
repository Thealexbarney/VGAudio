using System;
using DspAdpcm.Codecs;
using DspAdpcm.Utilities;

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
        
        private GcAdpcmSeekTable SeekTable { get; }
        private GcAdpcmLoopContext LoopContext { get; }
        private GcAdpcmAlignment Alignment { get; } = new GcAdpcmAlignment();
        private bool AlignmentNeeded { get; set; }

        public GcAdpcmChannel(int sampleCount)
        {
            _sampleCount = sampleCount;
            AudioData = new byte[GcAdpcmHelpers.SampleCountToByteCount(sampleCount)];
            LoopContext = new GcAdpcmLoopContext(this);
            SeekTable = new GcAdpcmSeekTable(this);
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
            SeekTable = new GcAdpcmSeekTable(this);
        }

        public short[] GetPcmAudio(bool includeHistorySamples = false) =>
            GcAdpcmDecoder.Decode(this, 0, SampleCount, includeHistorySamples);

        public short[] GetPcmAudioLooped(int startSample, int length, int loopStart, int loopEnd,
            bool includeHistorySamples = false)
        {
            short[] output = new short[length + (includeHistorySamples ? 2 : 0)];
            int outIndex = 0;
            int samplesRemaining = length;
            int currentSample = GetLoopedSample(startSample, loopStart, loopEnd);
            bool firstTime = true;

            while (samplesRemaining > 0 || firstTime && includeHistorySamples)
            {
                int samplesToGet = Math.Min(loopEnd - currentSample, samplesRemaining);
                short[] samples = GcAdpcmDecoder.Decode(this, currentSample, samplesToGet, firstTime && includeHistorySamples);
                Array.Copy(samples, 0, output, outIndex, samples.Length);
                samplesRemaining -= samplesToGet;
                outIndex += samples.Length;
                currentSample = loopStart;
                firstTime = false;
            }

            return output;
        }

        public short[] GetSeekTable(int samplesPerEntry, bool ensureSelfCalculated = false)
            => SeekTable.GetSeekTable(samplesPerEntry, ensureSelfCalculated);

        public void AddSeekTable(short[] table, int samplesPerEntry) => SeekTable.AddSeekTable(table, samplesPerEntry);

        internal void ClearSeekTableCache() => SeekTable.ClearSeekTableCache();

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

        internal Tuple<int, short, short> GetStartingHistory(int firstSample)
        {
            Tuple<int, short[]> seekInfo = SeekTable.GetTableForSeeking();
            if (seekInfo == null)
            {
                return new Tuple<int, short, short>(0, Hist1, Hist2);
            }

            short[] seekTable = seekInfo.Item2;
            int samplesPerEntry = seekInfo.Item1;

            int entry = firstSample / samplesPerEntry;
            while (entry * 2 + 1 > seekTable.Length)
                entry--;

            int sample = entry * samplesPerEntry;
            short hist1 = seekTable[entry * 2];
            short hist2 = seekTable[entry * 2 + 1];

            return new Tuple<int, short, short>(sample, hist1, hist2);
        }

        public void SetLoopContext(int loopStart, short predScale, short hist1, short hist2)
            => LoopContext.AddLoopContext(loopStart, predScale, hist1, hist2);

        public override bool Equals(object obj)
        {
            var item = obj as GcAdpcmChannel;

            if (item == null)
            {
                return false;
            }

            return
                item.SampleCount == SampleCount &&
                item.Gain == Gain &&
                item.Hist1 == Hist1 &&
                item.Hist2 == Hist2 &&
                Helpers.ArraysEqual(item.Coefs, Coefs) &&
                Helpers.ArraysEqual(item.AudioData, AudioData);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = SampleCount.GetHashCode();
                hashCode = (hashCode * 397) ^ Gain.GetHashCode();
                hashCode = (hashCode * 397) ^ Hist1.GetHashCode();
                hashCode = (hashCode * 397) ^ Hist2.GetHashCode();
                return hashCode;
            }
        }
    }
}
