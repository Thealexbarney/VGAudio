using DspAdpcm.Adpcm;
using DspAdpcm.Adpcm.Formats;
using Xunit;

namespace DspAdpcm.Tests
{
    public class BrstmTests
    {
        private AdpcmStream adpcm;

        public BrstmTests()
        {
            adpcm = GenerateAudio.GenerateAdpcmSineWave(48000, 440);
        }

        [Fact]
        public void BrstmBuildAndRead()
        {
            var brstm = new Brstm(adpcm);
            var brstmFile = brstm.GetFile();
            var readBrstm = new Brstm(brstmFile);
            Assert.Equal(adpcm, readBrstm.AudioStream);
        }
    }
}
