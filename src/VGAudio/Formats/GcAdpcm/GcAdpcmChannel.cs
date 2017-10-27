using System;
using VGAudio.Codecs.GcAdpcm;

namespace VGAudio.Formats.GcAdpcm
{
    public class GcAdpcmChannel
    {
        public byte[] Adpcm { get; }
        private short[] Pcm { get; }
        internal int UnalignedSampleCount { get; }
        public int SampleCount => AlignmentNeeded ? Alignment.SampleCountAligned : UnalignedSampleCount;

        public short Gain { get; }
        public short[] Coefs { get; }
        public GcAdpcmContext StartContext { get; }
        public GcAdpcmContext LoopContext => LoopContextEx;

        private GcAdpcmSeekTable SeekTable { get; }
        private GcAdpcmLoopContext LoopContextEx { get; }
        private GcAdpcmAlignment Alignment { get; }
        private int AlignmentMultiple => Alignment?.AlignmentMultiple ?? 0;
        private bool AlignmentNeeded => Alignment?.AlignmentNeeded ?? false;

        public GcAdpcmChannel(byte[] adpcm, short[] coefs, int sampleCount)
        {
            Adpcm = adpcm;
            Coefs = coefs;
            UnalignedSampleCount = sampleCount;
        }

        internal GcAdpcmChannel(GcAdpcmChannelBuilder b)
        {
            if (b.AlignedAdpcm.Length < GcAdpcmMath.SampleCountToByteCount(b.SampleCount))
            {
                throw new ArgumentException("Audio array length is too short for the specified number of samples.");
            }

            UnalignedSampleCount = b.SampleCount;
            Adpcm = b.Adpcm;
            Pcm = b.Pcm;

            Coefs = b.Coefs;
            Gain = b.Gain;
            StartContext = new GcAdpcmContext(Adpcm[0], b.StartContext.Hist1, b.StartContext.Hist2);

            Alignment = b.GetAlignment();
            LoopContextEx = b.GetLoopContext();
            SeekTable = b.GetSeekTable();

            //Grab the PCM data in case it was generated for the loop context or seek table
            if (!AlignmentNeeded)
            {
                Pcm = b.AlignedPcm;
            }
        }

        public short[] GetPcmAudio() => AlignmentNeeded
            ? Alignment.PcmAligned
            : Pcm ?? GcAdpcmDecoder.Decode(GetAdpcmAudio(), Coefs,
                  new GcAdpcmParameters {SampleCount = SampleCount, History1 = StartContext.Hist1, History2 = StartContext.Hist2});
        public short[] GetSeekTable() => SeekTable?.SeekTable ?? new short[0];
        public byte[] GetAdpcmAudio() => AlignmentNeeded ? Alignment.AdpcmAligned : Adpcm;

        public byte GetPredScale(int sampleNum) => GcAdpcmLoopContext.GetPredScale(GetAdpcmAudio(), sampleNum);
        public short GetHist1(int sampleNum) => GcAdpcmLoopContext.GetHist1(Pcm, sampleNum);
        public short GetHist2(int sampleNum) => GcAdpcmLoopContext.GetHist2(Pcm, sampleNum);

        public GcAdpcmChannelBuilder GetCloneBuilder()
        {
            var builder = new GcAdpcmChannelBuilder(Adpcm, Coefs, UnalignedSampleCount)
            {
                Pcm = Pcm,
                Gain = Gain,
                StartContext = StartContext,
                LoopAlignmentMultiple = AlignmentMultiple
            };
            builder.WithPrevious(SeekTable, LoopContextEx, Alignment);

            if (SeekTable != null)
            {
                builder.WithSeekTable(SeekTable.SeekTable, SeekTable.SamplesPerEntry, SeekTable.IsSelfCalculated);
            }

            if (LoopContextEx != null)
            {
                builder.WithLoopContext(LoopContextEx.LoopStart, StartContext.PredScale, StartContext.Hist1, StartContext.Hist2);
            }

            if (Alignment != null)
            {
                builder.WithLoop(true, Alignment.LoopStart, Alignment.LoopEnd);
            }

            return builder;
        }
    }
}
