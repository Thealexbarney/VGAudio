using System;
using DspAdpcm.Adpcm;
using DspAdpcm.Pcm;
using Xunit;

namespace DspAdpcm.Tests
{
    public static class BuildParseTests
    {
        public static void BuildParseCompareAdpcm(Func<AdpcmStream, byte[]> buildFunc, Func<byte[], AdpcmStream> parseFunc, AdpcmStream adpcm)
        {
            var builtFile = buildFunc(adpcm);
            var parsedAdpcm = parseFunc(builtFile);
            Assert.Equal(adpcm, parsedAdpcm);
        }

        public static void BuildParseComparePcm(Func<PcmStream, byte[]> buildFunc, Func<byte[], PcmStream> parseFunc, PcmStream pcm)
        {
            var builtFile = buildFunc(pcm);
            var parsedPcm = parseFunc(builtFile);
            Assert.Equal(pcm, parsedPcm);
        }
    }
}
