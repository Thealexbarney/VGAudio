namespace VGAudio.Formats.GcAdpcm
{
    public class GcAdpcmChannelBuilder
    {
        public byte[] Adpcm { get; }
        public short[] Coefs { get; }
        internal short[] Pcm { get; set; }
        public int SampleCount { get; }
        public short Gain { get; set; }
        public short Hist1 { get; set; }
        public short Hist2 { get; set; }
        public int LoopAlignmentMultiple { get; set; }

        internal bool EnsureSeekTableIsSelfCalculated { get; set; }
        internal bool EnsureLoopContextIsSelfCalculated { get; set; }

        internal short[] SeekTable { get; private set; }
        internal int SamplesPerSeekTableEntry { get; private set; }
        internal bool SeekTableIsSelfCalculated { get; private set; }

        internal short LoopPredScale { get; private set; }
        internal short LoopHist1 { get; private set; }
        internal short LoopHist2 { get; private set; }
        internal bool LoopContextIsSelfCalculated { get; private set; }


        internal bool Looping { get; private set; }
        internal int LoopStart { get; private set; }
        internal int LoopEnd { get; private set; }

        internal GcAdpcmSeekTable PreviousSeekTable { get; private set; }
        internal GcAdpcmLoopContext PreviousLoopContext { get; private set; }
        internal GcAdpcmAlignment PreviousAlignment { get; private set; }

        public GcAdpcmChannel Build() => new GcAdpcmChannel(this);

        public GcAdpcmChannelBuilder(byte[] adpcm, short[] coefs, int sampleCount)
        {
            Adpcm = adpcm;
            Coefs = coefs;
            SampleCount = sampleCount;
        }

        public GcAdpcmChannelBuilder SetSeekTable(short[] seekTable, int samplesPerEntry, bool isSelfCalculated = false)
        {
            SeekTable = seekTable;
            SamplesPerSeekTableEntry = samplesPerEntry;
            SeekTableIsSelfCalculated = isSelfCalculated;
            PreviousSeekTable = null;
            return this;
        }

        public GcAdpcmChannelBuilder SetSeekTable(int samplesPerEntry)
        {
            if (samplesPerEntry != SamplesPerSeekTableEntry)
            {
                SeekTable = null;
                SeekTableIsSelfCalculated = false;
            }
            SamplesPerSeekTableEntry = samplesPerEntry;
            return this;
        }

        public GcAdpcmChannelBuilder SetLoopContext(int loopStart, short predScale, short loopHist1, short loopHist2, bool isSelfCalculated = false)
        {
            LoopStart = loopStart;
            LoopPredScale = predScale;
            LoopHist1 = loopHist1;
            LoopHist2 = loopHist2;
            LoopContextIsSelfCalculated = isSelfCalculated;
            PreviousLoopContext = null;
            return this;
        }

        public GcAdpcmChannelBuilder SetLoop(bool loop, int loopStart, int loopEnd)
        {
            if (!loop)
            {
                return SetLoop(false);
            }

            Looping = true;
            LoopStart = loopStart;
            LoopEnd = loopEnd;
            PreviousAlignment = null;
            return this;
        }

        public GcAdpcmChannelBuilder SetLoop(bool loop)
        {
            Looping = loop;
            LoopStart = 0;
            LoopEnd = loop ? SampleCount : 0;
            PreviousAlignment = null;
            return this;
        }

        internal GcAdpcmChannelBuilder SetPrevious(GcAdpcmSeekTable seekTable, GcAdpcmLoopContext loopContext, GcAdpcmAlignment alignment)
        {
            PreviousSeekTable = seekTable;
            PreviousLoopContext = loopContext;
            PreviousAlignment = alignment;
            return this;
        }
    }
}
