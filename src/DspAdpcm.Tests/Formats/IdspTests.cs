using System;
using DspAdpcm.Adpcm;
using DspAdpcm.Adpcm.Formats;
using Xunit;

namespace DspAdpcm.Tests.Formats
{
    public class IdspTests
    {
        private static readonly Func<AdpcmStream, byte[]> BuildFunc = adpcmStream => new Idsp(adpcmStream).GetFile();
        private static readonly Func<byte[], AdpcmStream> ParseFunc = file => new Idsp(file).AudioStream;

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(8)]
        public void IdspBuildAndParseEqual(int numChannels)
        {
            var adpcm = GenerateAudio.GenerateAdpcmSineWave(BuildParseTestOptions.Samples, numChannels, BuildParseTestOptions.SampleRate);
            BuildParseTests.BuildParseCompareAdpcm(BuildFunc, ParseFunc, adpcm);
        }
    }
}
