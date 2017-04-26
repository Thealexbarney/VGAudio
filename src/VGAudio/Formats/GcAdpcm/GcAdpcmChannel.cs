using System;
using VGAudio.Codecs;

namespace VGAudio.Formats.GcAdpcm
{
    public class GcAdpcmChannel
    {
        private readonly int _sampleCount;

        public byte[] Adpcm { get; }
        private short[] Pcm { get; }
        public int SampleCount => AlignmentNeeded ? Alignment.SampleCountAligned : _sampleCount;

        public short Gain { get; }
        public short[] Coefs { get; }
        public short PredScale => Adpcm[0];
        public short Hist1 { get; }
        public short Hist2 { get; }

        public short LoopPredScale => LoopContext?.PredScale ?? 0;
        public short LoopHist1 => LoopContext?.Hist1 ?? 0;
        public short LoopHist2 => LoopContext?.Hist2 ?? 0;

        private GcAdpcmSeekTable SeekTable { get; }
        private GcAdpcmLoopContext LoopContext { get; }
        private GcAdpcmAlignment Alignment { get; }
        private int AlignmentMultiple => Alignment?.AlignmentMultiple ?? 0;
        private bool AlignmentNeeded => Alignment?.AlignmentNeeded ?? false;

        public GcAdpcmChannel(byte[] adpcm, short[] coefs, int sampleCount)
        {
            Adpcm = adpcm;
            Coefs = coefs;
            _sampleCount = sampleCount;
        }

        internal GcAdpcmChannel(GcAdpcmChannelBuilder b)
        {
            if (b.Adpcm.Length < GcAdpcmHelpers.SampleCountToByteCount(b.SampleCount))
            {
                throw new ArgumentException("Audio array length is too short for the specified number of samples.");
            }

            _sampleCount = b.SampleCount;
            Adpcm = b.Adpcm;
            short[] pcm = b.Pcm;

            Coefs = b.Coefs;
            Gain = b.Gain;
            Hist1 = b.Hist1;
            Hist2 = b.Hist2;

            Alignment = CreateAlignment(b);
            if (AlignmentNeeded)
            {
                pcm = Alignment.Pcm;
            }
            LoopContext = CreateLoopContext(b, ref pcm, Alignment.LoopStartAligned);
            SeekTable = CreateSeekTable(b, ref pcm);

            Pcm = pcm;
        }

        private void EnsurePcmDecoded(ref short[] pcm) => pcm = pcm ?? GcAdpcmDecoder.Decode(Adpcm, Coefs, SampleCount);

        internal GcAdpcmAlignment CreateAlignment(GcAdpcmChannelBuilder b)
        {
            GcAdpcmAlignment previous = b.PreviousAlignment;

            if (b.Looping && previous?.LoopStart == b.LoopStart && previous.LoopEnd == b.LoopEnd &&
                previous.AlignmentMultiple == b.LoopAlignmentMultiple)
            {
                return previous;
            }

            return new GcAdpcmAlignment(b.LoopAlignmentMultiple, b.LoopStart, b.LoopEnd, Adpcm, Coefs);
        }

        internal GcAdpcmLoopContext CreateLoopContext(GcAdpcmChannelBuilder b, ref short[] pcm, int loopStartAligned)
        {
            GcAdpcmLoopContext previous = b.PreviousLoopContext;

            if (previous?.LoopStart == loopStartAligned && (!b.EnsureLoopContextIsSelfCalculated || previous.IsSelfCalculated))
            {
                return previous;
            }

            if (b.LoopContextStart == loopStartAligned && (!b.EnsureLoopContextIsSelfCalculated || b.LoopContextIsSelfCalculated))
            {
                return new GcAdpcmLoopContext(b.LoopPredScale, b.LoopHist1, b.LoopHist2, b.LoopContextStart, b.LoopContextIsSelfCalculated);
            }

            EnsurePcmDecoded(ref pcm);
            return new GcAdpcmLoopContext(Adpcm, pcm, loopStartAligned);
        }

        internal GcAdpcmSeekTable CreateSeekTable(GcAdpcmChannelBuilder b, ref short[] pcm)
        {
            if (b.SamplesPerSeekTableEntry == 0)
            {
                return null;
            }

            GcAdpcmSeekTable previous = b.PreviousSeekTable;
            if (previous?.SamplesPerEntry == b.SamplesPerSeekTableEntry && (!b.EnsureSeekTableIsSelfCalculated || previous.IsSelfCalculated))
            {
                return previous;
            }

            if (b.SeekTable != null && (!b.EnsureSeekTableIsSelfCalculated || b.SeekTableIsSelfCalculated))
            {
                return new GcAdpcmSeekTable(b.SeekTable, b.SamplesPerSeekTableEntry, b.SeekTableIsSelfCalculated);
            }
            EnsurePcmDecoded(ref pcm);
            return new GcAdpcmSeekTable(pcm, b.SamplesPerSeekTableEntry);
        }

        public short[] GetPcmAudio() => Pcm ?? GcAdpcmDecoder.Decode(GetAudioData(), Coefs, SampleCount, Hist1, Hist2);
        public short[] GetSeekTable() => SeekTable?.SeekTable ?? new short[0];
        public byte[] GetAudioData() => AlignmentNeeded ? Alignment.AdpcmAligned : Adpcm;

        public GcAdpcmChannelBuilder GetCloneBuilder()
        {
            var builder = new GcAdpcmChannelBuilder(Adpcm, Coefs, SampleCount)
            {
                Pcm = Pcm,
                Gain = Gain,
                Hist1 = Hist1,
                Hist2 = Hist2,
                LoopAlignmentMultiple = AlignmentMultiple
            };
            builder.SetPrevious(SeekTable, LoopContext, Alignment);

            if (SeekTable != null)
            {
                builder.SetSeekTable(SeekTable.SeekTable, SeekTable.SamplesPerEntry, SeekTable.IsSelfCalculated);
            }

            if (LoopContext != null)
            {
                builder.SetLoopContext(LoopContext.LoopStart, PredScale, Hist1, Hist2);
            }

            if (Alignment != null)
            {
                builder.SetLoop(true, Alignment.LoopStart, Alignment.LoopEnd);
            }

            return builder;
        }
    }
}
