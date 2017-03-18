using VGAudio.Formats;
using Xunit;
using static VGAudio.Formats.GcAdpcm.GcAdpcmHelpers;

namespace VGAudio.Tests.Formats
{
    public class GcAdpcmFormatTests
    {
        [Theory]
        [InlineData(100, true, 30, 90)]
        [InlineData(100, false, 0, 0)]
        public void LoopsProperlyAfterEncoding(int sampleCount, bool looping, int loopStart, int loopEnd)
        {
            Pcm16Format pcm = GenerateAudio.GeneratePcmSineWave(sampleCount, 1, 48000)
                .SetLoop(looping, loopStart, loopEnd);

            GcAdpcmFormat adpcm = new GcAdpcmFormat().EncodeFromPcm16(pcm);
            Assert.Equal(looping, adpcm.Looping);
            Assert.Equal(loopStart, adpcm.LoopStart);
            Assert.Equal(loopEnd, adpcm.LoopEnd);
        }

        [Theory]
        [InlineData(100, true, 30, 90)]
        [InlineData(100, false, 0, 0)]
        public void LoopsProperlyAfterDecoding(int sampleCount, bool looping, int loopStart, int loopEnd)
        {
            GcAdpcmFormat adpcm = GenerateAudio.GenerateAdpcmEmpty(sampleCount, 1, 48000)
                .SetLoop(looping, loopStart, loopEnd);

            Pcm16Format pcm = adpcm.ToPcm16();
            Assert.Equal(looping, pcm.Looping);
            Assert.Equal(loopStart, pcm.LoopStart);
            Assert.Equal(loopEnd, pcm.LoopEnd);
        }

        [Theory]
        [InlineData(100, true, 30, 90)]
        public void AdpcmDataLengthIsCorrectAfterLooping(int sampleCount, bool looping, int loopStart, int loopEnd)
        {
            GcAdpcmFormat adpcm = GenerateAudio.GenerateAdpcmEmpty(sampleCount, 1, 48000)
                .SetLoop(looping, loopStart, loopEnd);

            Assert.Equal(SampleCountToByteCount(sampleCount), adpcm.Channels[0].GetAudioData().Length);
        }

        [Theory]
        [InlineData(100, true, 30, 90, 50)]
        public void AdpcmDataLengthIsCorrectAfterUnlooping(int sampleCount, bool looping, int loopStart, int loopEnd, int alignment)
        {
            GcAdpcmFormat adpcm = GenerateAudio.GenerateAdpcmEmpty(sampleCount, 1, 48000)
                .SetAlignment(alignment)
                .SetLoop(looping, loopStart, loopEnd)
                .SetLoop(false);

            Assert.Equal(SampleCountToByteCount(sampleCount), adpcm.Channels[0].GetAudioData().Length);
        }

        [Theory]
        [InlineData(100, true, 30, 90, 50)]
        public void AdpcmDataLengthIsCorrectAfterAlignment(int sampleCount, bool looping, int loopStart, int loopEnd, int alignment)
        {
            GcAdpcmFormat adpcm = GenerateAudio.GenerateAdpcmEmpty(sampleCount, 1, 48000)
                .SetAlignment(alignment)
                .SetLoop(looping, loopStart, loopEnd);

            int extraSamples = Utilities.Helpers.GetNextMultiple(loopStart, alignment) - loopStart;

            Assert.Equal(SampleCountToByteCount(loopEnd + extraSamples), adpcm.Channels[0].GetAudioData().Length);
        }

        [Theory]
        [InlineData(100, true, 30, 90, 50)]
        public void AdpcmDataLengthIsCorrectAfterUnalignment(int sampleCount, bool looping, int loopStart, int loopEnd, int alignment)
        {
            GcAdpcmFormat adpcm = GenerateAudio.GenerateAdpcmEmpty(sampleCount, 1, 48000)
                .SetAlignment(alignment)
                .SetLoop(looping, loopStart, loopEnd)
                .SetAlignment(0);

            Assert.Equal(SampleCountToByteCount(sampleCount), adpcm.Channels[0].GetAudioData().Length);
        }

        [Theory]
        [InlineData(100, true, 30, 90, 50)]
        public void AdpcmLoopIsCorrectAfterAlignment(int sampleCount, bool looping, int loopStart, int loopEnd, int alignment)
        {
            GcAdpcmFormat adpcm = GenerateAudio.GenerateAdpcmEmpty(sampleCount, 1, 48000)
                .SetAlignment(alignment)
                .SetLoop(looping, loopStart, loopEnd);

            int extraSamples = Utilities.Helpers.GetNextMultiple(loopStart, alignment) - loopStart;

            Assert.Equal(loopStart + extraSamples, adpcm.LoopStart);
            Assert.Equal(loopEnd + extraSamples, adpcm.LoopEnd);
        }

        [Theory]
        [InlineData(100, true, 30, 90, 50)]
        public void AdpcmLoopIsCorrectAfterUnalignment(int sampleCount, bool looping, int loopStart, int loopEnd, int alignment)
        {
            GcAdpcmFormat adpcm = GenerateAudio.GenerateAdpcmEmpty(sampleCount, 1, 48000)
                .SetAlignment(alignment)
                .SetLoop(looping, loopStart, loopEnd)
                .SetAlignment(0);

            Assert.Equal(loopStart, adpcm.LoopStart);
            Assert.Equal(loopEnd, adpcm.LoopEnd);
        }

        [Theory]
        [InlineData(100, true, 30, 90)]
        public void AdpcmSampleCountIsCorrectAfterLooping(int sampleCount, bool looping, int loopStart, int loopEnd)
        {
            GcAdpcmFormat adpcm = GenerateAudio.GenerateAdpcmEmpty(sampleCount, 1, 48000)
                .SetLoop(looping, loopStart, loopEnd);

            Assert.Equal(sampleCount, adpcm.SampleCount);
        }
    }
}
