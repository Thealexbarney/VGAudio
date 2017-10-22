using VGAudio.Containers.Idsp;
using VGAudio.Formats.GcAdpcm;
using Xunit;

namespace VGAudio.Tests.Containers
{
    public class IdspTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(8)]
        public void IdspBuildAndParseEqual(int numChannels)
        {
            GcAdpcmFormat audio = GenerateAudio.GenerateAdpcmSineWave(BuildParseTestOptions.Samples, numChannels, BuildParseTestOptions.SampleRate);

            BuildParseTests.BuildParseCompareAudio(audio, new IdspWriter(), new IdspReader());
        }

        [Theory]
        [InlineData(true, 1234, 2000, 1260, 2026, 0x10)]
        [InlineData(true, 1248, 2014, 1260, 2026, 0x10)]
        [InlineData(true, 1234, 2000, 1274, 2040, 0x38)]
        [InlineData(true, 1274, 2040, 1274, 2040, 0x38)]
        [InlineData(false, 0, 0, 0, 0, 0x10)]
        public void IdspAlignsLoopToBlock(bool loops, int startIn, int endIn, int startOut, int endOut, int blockSize)
        {
            GcAdpcmFormat audio = GenerateAudio.GenerateAdpcmSineWave(BuildParseTestOptions.Samples, 2, BuildParseTestOptions.SampleRate);
            audio = audio.WithLoop(loops, startIn, endIn);

            var config = new IdspConfiguration { BytesPerInterleave = blockSize };

            byte[] idsp = new IdspWriter().GetFile(audio, config);
            var decoded = (GcAdpcmFormat)new IdspReader().ReadFormat(idsp);
            Assert.Equal(startOut, decoded.LoopStart);
            Assert.Equal(endOut, decoded.LoopEnd);
        }
    }
}
