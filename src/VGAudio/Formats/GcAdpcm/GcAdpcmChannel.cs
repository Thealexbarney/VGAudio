using System;
using VGAudio.Codecs;
using VGAudio.Utilities;

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

            int loopStart = b.LoopStart;
            int loopEnd = b.LoopEnd;

            Alignment = CreateAlignment();
            if (AlignmentNeeded)
            {
                pcm = Alignment.Pcm;
            }
            LoopContext = CreateLoopContext();
            SeekTable = CreateSeekTable();

            Pcm = pcm;

            GcAdpcmAlignment CreateAlignment()
            {
                GcAdpcmAlignment previous = b.PreviousAlignment;

                if (b.Looping && previous?.LoopStart == loopStart && previous.LoopEnd == loopEnd &&
                    previous.AlignmentMultiple == b.LoopAlignmentMultiple)
                {
                    return previous;
                }

                return new GcAdpcmAlignment(b.LoopAlignmentMultiple, loopStart, loopEnd, Adpcm, Coefs);
            }

            GcAdpcmLoopContext CreateLoopContext()
            {
                GcAdpcmLoopContext previous = b.PreviousLoopContext;

                if (previous?.LoopStart == Alignment.LoopStartAligned && (b.EnsureLoopContextIsSelfCalculated && previous.IsSelfCalculated || !b.EnsureLoopContextIsSelfCalculated))
                {
                    return previous;
                }

                if (b.EnsureLoopContextIsSelfCalculated && b.LoopContextIsSelfCalculated ||
                    !b.EnsureLoopContextIsSelfCalculated)
                {
                    return new GcAdpcmLoopContext(b.LoopPredScale, b.LoopHist1, b.LoopHist2, Alignment.LoopStartAligned, b.LoopContextIsSelfCalculated);
                }

                EnsurePcmDecoded();
                return new GcAdpcmLoopContext(Adpcm, pcm, Alignment.LoopStartAligned);
            }

            GcAdpcmSeekTable CreateSeekTable()
            {
                if (b.SamplesPerSeekTableEntry == 0)
                {
                    return null;
                }

                GcAdpcmSeekTable previous = b.PreviousSeekTable;
                if (previous?.SamplesPerEntry == b.SamplesPerSeekTableEntry && (b.EnsureSeekTableIsSelfCalculated && previous.IsSelfCalculated || !b.EnsureSeekTableIsSelfCalculated))
                {
                    return previous;
                }

                if (b.SeekTable != null && (b.EnsureSeekTableIsSelfCalculated && b.SeekTableIsSelfCalculated ||
                                            !b.EnsureSeekTableIsSelfCalculated))
                {
                    return new GcAdpcmSeekTable(b.SeekTable, b.SamplesPerSeekTableEntry, b.SeekTableIsSelfCalculated);
                }
                EnsurePcmDecoded();
                return new GcAdpcmSeekTable(pcm, b.SamplesPerSeekTableEntry);
            }

            void EnsurePcmDecoded() => pcm = pcm ?? GcAdpcmDecoder.Decode(Adpcm, Coefs, SampleCount);
        }

        public short[] GetPcmAudio(bool includeHistorySamples = false) =>
            GcAdpcmDecoder.Decode(this, 0, SampleCount, includeHistorySamples);

        public short[] GetSeekTable(int samplesPerEntry, bool ensureSelfCalculated = false)
            => SeekTable?.SeekTable ?? new short[0];

        public byte[] GetAudioData()
        {
            return AlignmentNeeded ? Alignment.AdpcmAligned : Adpcm;
        }

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
                builder.SetLoop(Alignment.LoopStart, Alignment.LoopEnd);
            }
            
            return builder;
        }

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
                Helpers.ArraysEqual(item.Adpcm, Adpcm);
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
