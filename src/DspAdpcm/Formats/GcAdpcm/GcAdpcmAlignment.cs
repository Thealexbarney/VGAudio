using System;
using DspAdpcm.Codecs;
using DspAdpcm.Utilities;
using static DspAdpcm.Formats.GcAdpcm.GcAdpcmHelpers;

namespace DspAdpcm.Formats.GcAdpcm
{
    internal class GcAdpcmAlignment
    {
        public byte[] AudioDataAligned { get; private set; }

        private int AlignmentMultiple { get; set; }
        private int LoopStart { get; set; }
        private int LoopEnd { get; set; }
        public int SampleCountAligned { get; private set; }

        public bool SetAlignment(int multiple, int loopStart, int loopEnd, GcAdpcmChannel audio, int samplesPerSeekTableEntry = 0x3800)
        {
            if (multiple == 0 || loopStart % multiple == 0)
            {
                AudioDataAligned = null;
                AlignmentMultiple = multiple;
                LoopStart = loopStart;
                LoopEnd = loopEnd;
                SampleCountAligned = audio.SampleCount;
                return false;
            }

            if (AlignmentMultiple == multiple
                && LoopStart == loopStart
                && LoopEnd == loopEnd)
            {
                return true;
            }

            int loopStartAligned = Helpers.GetNextMultiple(loopStart, multiple);
            SampleCountAligned = loopEnd + (loopStartAligned - loopStart);

            int newAudioDataLength = SampleCountToByteCount(SampleCountAligned);
            AudioDataAligned = new byte[newAudioDataLength];

            int framesToCopy = loopEnd / SamplesPerFrame;
            int bytesToCopy = framesToCopy * BytesPerFrame; 
            int samplesToCopy = framesToCopy * SamplesPerFrame;
            Array.Copy(audio.AudioData, 0, AudioDataAligned, 0, bytesToCopy);

            //We're gonna be doing some seeking, so make sure the seek table is built
            audio.GetSeekTable(samplesPerSeekTableEntry, true);

            int samplesToEncode = SampleCountAligned - samplesToCopy;

            short[] history = audio.GetPcmAudioLooped(samplesToCopy, 16, loopStart, loopEnd, true);
            short[] pcm = audio.GetPcmAudioLooped(samplesToCopy, samplesToEncode, loopStart, loopEnd);
            var adpcm = GcAdpcmEncoder.EncodeAdpcm(pcm, audio.Coefs, samplesToEncode, history[1], history[0]);

            Array.Copy(adpcm, 0, AudioDataAligned, bytesToCopy, adpcm.Length);

            AlignmentMultiple = multiple;
            LoopStart = loopStart;
            LoopEnd = loopEnd;

            audio.ClearSeekTables();

            return true;
        }
    }
}
