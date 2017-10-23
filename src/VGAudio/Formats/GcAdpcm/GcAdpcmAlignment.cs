using System;
using VGAudio.Codecs.GcAdpcm;
using VGAudio.Utilities;
using static VGAudio.Codecs.GcAdpcm.GcAdpcmMath;

namespace VGAudio.Formats.GcAdpcm
{
    internal class GcAdpcmAlignment
    {
        public byte[] AdpcmAligned { get; }
        public short[] PcmAligned { get; }

        public int AlignmentMultiple { get; }
        public int LoopStart { get; }
        public int LoopStartAligned { get; }
        public int LoopEnd { get; }
        public int SampleCountAligned { get; }
        public bool AlignmentNeeded { get; }

        public GcAdpcmAlignment(int multiple, int loopStart, int loopEnd, byte[] adpcm, short[] coefs)
        {
            AlignmentMultiple = multiple;
            LoopStart = loopStart;
            LoopEnd = loopEnd;
            AlignmentNeeded = !Helpers.LoopPointsAreAligned(loopStart, multiple);

            if (!AlignmentNeeded) return;

            int loopLength = loopEnd - loopStart;
            LoopStartAligned = Helpers.GetNextMultiple(loopStart, multiple);
            SampleCountAligned = loopEnd + (LoopStartAligned - loopStart);

            AdpcmAligned = new byte[SampleCountToByteCount(SampleCountAligned)];
            PcmAligned = new short[SampleCountAligned];

            int framesToKeep = loopEnd / SamplesPerFrame;
            int bytesToKeep = framesToKeep * BytesPerFrame;
            int samplesToKeep = framesToKeep * SamplesPerFrame;
            int samplesToEncode = SampleCountAligned - samplesToKeep;

            var param = new GcAdpcmParameters { SampleCount = loopEnd };
            short[] oldPcm = GcAdpcmDecoder.Decode(adpcm, coefs, param);
            Array.Copy(oldPcm, 0, PcmAligned, 0, loopEnd);
            var newPcm = new short[samplesToEncode];

            Array.Copy(oldPcm, samplesToKeep, newPcm, 0, loopEnd - samplesToKeep);

            for (int currentSample = loopEnd - samplesToKeep; currentSample < samplesToEncode; currentSample += loopLength)
            {
                Array.Copy(PcmAligned, loopStart, newPcm, currentSample, Math.Min(loopLength, samplesToEncode - currentSample));
            }

            param.SampleCount = samplesToEncode;
            param.History1 = samplesToKeep < 1 ? (short)0 : oldPcm[samplesToKeep - 1];
            param.History2 = samplesToKeep < 2 ? (short)0 : oldPcm[samplesToKeep - 2];

            byte[] newAdpcm = GcAdpcmEncoder.Encode(newPcm, coefs, param);
            Array.Copy(adpcm, 0, AdpcmAligned, 0, bytesToKeep);
            Array.Copy(newAdpcm, 0, AdpcmAligned, bytesToKeep, newAdpcm.Length);

            short[] decodedPcm = GcAdpcmDecoder.Decode(newAdpcm, coefs, param);
            Array.Copy(decodedPcm, 0, PcmAligned, samplesToKeep, samplesToEncode);
        }
    }
}
