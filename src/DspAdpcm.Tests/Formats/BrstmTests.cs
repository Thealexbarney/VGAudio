using System;
using DspAdpcm.Adpcm;
using DspAdpcm.Adpcm.Formats;
using DspAdpcm.Pcm;
using Xunit;

namespace DspAdpcm.Tests.Formats
{
    public class BrstmTests
    {
        private static readonly Func<AdpcmStream, byte[]> BuildFunc = adpcmStream => new Brstm(adpcmStream).GetFile();
        private static readonly Func<byte[], AdpcmStream> ParseFunc = file => (AdpcmStream)new Brstm(file).AudioStream;

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(8)]
        public void BrstmBuildAndParseEqual(int numChannels)
        {
            var adpcm = GenerateAudio.GenerateAdpcmSineWave(BuildParseTestOptions.Samples, numChannels, BuildParseTestOptions.SampleRate);
            BuildParseTests.BuildParseCompareAdpcm(BuildFunc, ParseFunc, adpcm);
        }

        private static readonly Func<PcmStream, byte[]> PcmBuildFunc = pcmStream => new Brstm(pcmStream).GetFile();
        private static readonly Func<byte[], PcmStream> PcmParseFunc = file => (PcmStream)new Brstm(file).AudioStream;

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(8)]
        public void BrstmPcmBuildAndParseEqual(int numChannels) {
            var pcm = GenerateAudio.GeneratePcmSineWave(BuildParseTestOptions.Samples, numChannels, BuildParseTestOptions.SampleRate);
            BuildParseTests.BuildParseComparePcm(PcmBuildFunc, PcmParseFunc, pcm);
        }
    }
}
