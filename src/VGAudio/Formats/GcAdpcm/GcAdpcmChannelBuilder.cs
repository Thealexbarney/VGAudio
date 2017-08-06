using VGAudio.Codecs.GcAdpcm;

namespace VGAudio.Formats.GcAdpcm
{
    public class GcAdpcmChannelBuilder
    {
        public byte[] Adpcm { get; }
        public short[] Coefs { get; }
        internal short[] Pcm { get; set; }
        public int SampleCount { get; }

        public GcAdpcmContext StartContext { get; set; }
        public short Gain { get; set; }
        public int LoopAlignmentMultiple { get; set; }

        public bool EnsureSeekTableIsSelfCalculated { get; set; }
        public bool EnsureLoopContextIsSelfCalculated { get; set; }

        internal short[] SeekTable { get; private set; }
        internal int SamplesPerSeekTableEntry { get; private set; }
        internal bool SeekTableIsSelfCalculated { get; private set; }

        public GcAdpcmContext LoopContext { get; set; }
        internal int LoopContextStart { get; private set; }
        internal bool LoopContextIsSelfCalculated { get; private set; }

        internal bool Looping { get; private set; }
        internal int LoopStart { get; private set; }
        internal int LoopEnd { get; private set; }

        internal GcAdpcmSeekTable PreviousSeekTable { get; private set; }
        internal GcAdpcmLoopContext PreviousLoopContext { get; private set; }
        internal GcAdpcmAlignment PreviousAlignment { get; private set; }

        internal short[] AlignedPcm { get; set; }
        internal byte[] AlignedAdpcm { get; set; }
        internal int AlignedLoopStart { get; set; }
        internal int AlignedSampleCount { get; set; }

        public GcAdpcmChannel Build()
        {
            PrepareForBuild();
            return new GcAdpcmChannel(this);
        }

        internal GcAdpcmChannelBuilder PrepareForBuild()
        {
            AlignedPcm = Pcm;
            AlignedAdpcm = Adpcm;
            AlignedLoopStart = LoopStart;
            AlignedSampleCount = SampleCount;
            StartContext = StartContext ?? new GcAdpcmContext(0, 0, 0);
            LoopContext = LoopContext ?? new GcAdpcmContext(0, 0, 0);
            return this;
        }

        public GcAdpcmChannelBuilder(byte[] adpcm, short[] coefs, int sampleCount)
        {
            Adpcm = adpcm;
            Coefs = coefs;
            SampleCount = sampleCount;
        }

        public GcAdpcmChannelBuilder WithLoopAlignment(int loopAlignmentMultiple)
        {
            LoopAlignmentMultiple = loopAlignmentMultiple;
            return this;
        }

        public GcAdpcmChannelBuilder WithSeekTable(short[] seekTable, int samplesPerEntry, bool isSelfCalculated = false)
        {
            SeekTable = seekTable;
            SamplesPerSeekTableEntry = samplesPerEntry;
            SeekTableIsSelfCalculated = isSelfCalculated;
            return this;
        }

        public GcAdpcmChannelBuilder WithSamplesPerSeekTableEntry(int samplesPerEntry)
        {
            if (samplesPerEntry != SamplesPerSeekTableEntry)
            {
                SeekTable = null;
                SeekTableIsSelfCalculated = false;
            }
            SamplesPerSeekTableEntry = samplesPerEntry;
            return this;
        }

        public GcAdpcmChannelBuilder WithLoopContext(int loopStart, short predScale, short loopHist1, short loopHist2, bool isSelfCalculated = false)
        {
            LoopContextStart = loopStart;
            LoopContext = new GcAdpcmContext(predScale, loopHist1, loopHist2);
            LoopContextIsSelfCalculated = isSelfCalculated;
            return this;
        }

        public GcAdpcmChannelBuilder WithLoop(bool loop, int loopStart, int loopEnd)
        {
            if (!loop)
            {
                return WithLoop(false);
            }

            Looping = true;
            LoopStart = loopStart;
            LoopEnd = loopEnd;
            return this;
        }

        public GcAdpcmChannelBuilder WithLoop(bool loop)
        {
            Looping = loop;
            LoopStart = 0;
            LoopEnd = loop ? SampleCount : 0;
            return this;
        }

        internal GcAdpcmChannelBuilder WithPrevious(GcAdpcmSeekTable seekTable, GcAdpcmLoopContext loopContext, GcAdpcmAlignment alignment)
        {
            PreviousSeekTable = seekTable;
            PreviousLoopContext = loopContext;
            PreviousAlignment = alignment;
            return this;
        }

        internal bool PreviousAlignmentIsValid() => Looping &&
            PreviousAlignment?.LoopStart == LoopStart &&
            PreviousAlignment.LoopEnd == LoopEnd &&
            PreviousAlignment.AlignmentMultiple == LoopAlignmentMultiple;

        internal bool PreviousLoopContextIsValid(int loopStart) =>
            PreviousLoopContext?.LoopStart == loopStart
            && (!EnsureLoopContextIsSelfCalculated || PreviousLoopContext.IsSelfCalculated);

        internal bool CurrentLoopContextIsValid(int loopStart) =>
            LoopContextStart == loopStart
            && (!EnsureLoopContextIsSelfCalculated || LoopContextIsSelfCalculated);

        internal bool PreviousSeekTableIsValid() =>
            SeekTable == null
            && PreviousSeekTable?.SamplesPerEntry == SamplesPerSeekTableEntry
            && (!EnsureSeekTableIsSelfCalculated || PreviousSeekTable.IsSelfCalculated);

        internal bool CurrentSeekTableIsValid() =>
            SeekTable != null
            && (!EnsureSeekTableIsSelfCalculated || SeekTableIsSelfCalculated);

        internal GcAdpcmAlignment GetAlignment()
        {
            if (PreviousAlignmentIsValid())
            {
                return PreviousAlignment;
            }
            var alignment = new GcAdpcmAlignment(LoopAlignmentMultiple, LoopStart, LoopEnd, Adpcm, Coefs);

            if (alignment.AlignmentNeeded)
            {
                AlignedAdpcm = alignment.AdpcmAligned;
                AlignedPcm = alignment.PcmAligned;
                AlignedLoopStart = alignment.LoopStart;
            }

            return alignment;
        }

        internal GcAdpcmLoopContext GetLoopContext()
        {
            if (PreviousLoopContextIsValid(AlignedLoopStart))
            {
                return PreviousLoopContext;
            }

            if (CurrentLoopContextIsValid(AlignedLoopStart))
            {
                return new GcAdpcmLoopContext(LoopContext.PredScale, LoopContext.Hist1, LoopContext.Hist2, LoopContextStart, LoopContextIsSelfCalculated);
            }

            EnsurePcmDecoded();
            return new GcAdpcmLoopContext(Adpcm, AlignedPcm, AlignedLoopStart);
        }

        internal GcAdpcmSeekTable GetSeekTable()
        {
            if (SamplesPerSeekTableEntry == 0)
            {
                return null;
            }

            if (PreviousSeekTableIsValid())
            {
                return PreviousSeekTable;
            }

            if (CurrentSeekTableIsValid())
            {
                return new GcAdpcmSeekTable(SeekTable, SamplesPerSeekTableEntry, SeekTableIsSelfCalculated);
            }
            EnsurePcmDecoded();
            return new GcAdpcmSeekTable(AlignedPcm, SamplesPerSeekTableEntry);
        }

        private void EnsurePcmDecoded() => AlignedPcm = AlignedPcm ?? GcAdpcmDecoder.Decode(AlignedAdpcm, Coefs, new GcAdpcmParameters { SampleCount = AlignedSampleCount });
    }
}
