using System;
using DspAdpcm.Adpcm;
using DspAdpcm.Adpcm.Formats;
using Xunit;

namespace DspAdpcm.Tests.Formats
{
    public class BcstmTests
    {
        private static readonly Func<AdpcmStream, byte[]> BuildFunc = adpcmStream => new Bcstm(adpcmStream).GetFile();
        private static readonly Func<byte[], AdpcmStream> ParseFunc = file => new Bcstm(file).AudioStream;

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(8)]
        public void BcstmBuildAndParseEqual(int numChannels)
        {
            var adpcm = GenerateAudio.GenerateAdpcmSineWave(48000, numChannels);
            BuildParseTests.BuildParseCompareAdpcm(BuildFunc, ParseFunc, adpcm);
        }
    }
}
