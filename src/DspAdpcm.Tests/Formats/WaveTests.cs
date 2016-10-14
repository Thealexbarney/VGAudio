using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DspAdpcm.Pcm;
using DspAdpcm.Pcm.Formats;
using Xunit;

namespace DspAdpcm.Tests.Formats
{
    public class WaveTests
    {
        private static readonly Func<PcmStream, byte[]> BuildFunc = pcmStream => new Wave(pcmStream).GetFile();
        private static readonly Func<byte[], PcmStream> ParseFunc = file => new Wave(file).AudioStream;

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(8)]
        public void BrstmBuildAndParseEqual(int numChannels)
        {
            PcmStream pcm = GenerateAudio.GeneratePcmSineWave(BuildParseTestOptions.Samples, numChannels, BuildParseTestOptions.SampleRate);
            BuildParseTests.BuildParseComparePcm(BuildFunc, ParseFunc, pcm);
        }
    }
}
