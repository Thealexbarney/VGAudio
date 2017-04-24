using System;
using VGAudio.Codecs;
using VGAudio.Utilities;
using static VGAudio.Formats.GcAdpcm.GcAdpcmHelpers;

namespace VGAudio.Formats.GcAdpcm
{
    internal class GcAdpcmAlignment
    {
        public byte[] Adpcm { get; }
        public byte[] AdpcmAligned { get; }
        public short[] Pcm { get; }

        public int AlignmentMultiple { get; }
        public int LoopStart { get; }
        public int LoopStartAligned { get; }
        public int LoopEnd { get; }
        public int SampleCountAligned { get; }
        public bool AlignmentNeeded { get; }

        public GcAdpcmAlignment(int multiple, int loopStart, int loopEnd, byte[] adpcm, short[] coefs)
        {
            Adpcm = adpcm;
            AlignmentMultiple = multiple;
            LoopStart = loopStart;
            LoopEnd = loopEnd;
            AlignmentNeeded = !Helpers.LoopPointsAreAligned(loopStart, multiple);

            if (!AlignmentNeeded) return;

            int loopLength = loopEnd - loopStart;
            LoopStartAligned = Helpers.GetNextMultiple(loopStart, multiple);
            SampleCountAligned = loopEnd + (LoopStartAligned - loopStart);

            AdpcmAligned = new byte[SampleCountToByteCount(SampleCountAligned)];
            Pcm = new short[SampleCountAligned];

            int framesToKeep = loopEnd / SamplesPerFrame;
            int bytesToKeep = framesToKeep * BytesPerFrame;
            int samplesToKeep = framesToKeep * SamplesPerFrame;
            int samplesToEncode = SampleCountAligned - samplesToKeep;

            short[] oldPcm = GcAdpcmDecoder.Decode(adpcm, coefs, loopEnd);
            Array.Copy(oldPcm, 0, Pcm, 0, loopEnd);
            var newPcm = new short[samplesToEncode];

            Array.Copy(oldPcm, samplesToKeep, newPcm, 0, loopEnd - samplesToKeep);

            for (int currentSample = loopEnd - samplesToKeep; currentSample < samplesToEncode; currentSample += loopLength)
            {
                Array.Copy(Pcm, loopStart, newPcm, currentSample, Math.Min(loopLength, samplesToEncode - currentSample));
            }

            short hist1 = samplesToKeep < 1 ? (short)0 : oldPcm[samplesToKeep - 1];
            short hist2 = samplesToKeep < 2 ? (short)0 : oldPcm[samplesToKeep - 2];
            byte[] newAdpcm = GcAdpcmEncoder.EncodeAdpcm(newPcm, coefs, samplesToEncode, hist1, hist2);
            Array.Copy(adpcm, 0, AdpcmAligned, 0, bytesToKeep);
            Array.Copy(newAdpcm, 0, AdpcmAligned, bytesToKeep, newAdpcm.Length);

            short[] decodedPcm = GcAdpcmDecoder.Decode(newAdpcm, coefs, samplesToEncode);
            Array.Copy(decodedPcm, 0, Pcm, samplesToKeep, samplesToEncode);
        }
    }
}
