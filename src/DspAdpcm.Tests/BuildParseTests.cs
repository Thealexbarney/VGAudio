using System;
using DspAdpcm.Adpcm;
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
    }
}
