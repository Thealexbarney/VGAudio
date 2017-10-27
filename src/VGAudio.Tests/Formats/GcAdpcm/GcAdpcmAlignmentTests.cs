using System;
using VGAudio.Codecs.GcAdpcm;
using Xunit;
using VGAudio.Formats.GcAdpcm;
using static VGAudio.Codecs.GcAdpcm.GcAdpcmMath;
using static VGAudio.Tests.GenerateAudio;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Tests.Formats.GcAdpcm
{
    public class GcAdpcmAlignmentTests
    {
        [Theory]
        [InlineData(0, 0, 10, 0x40)]
        [InlineData(0, 5, 10, 0x40)]
        [InlineData(1, 7, 10, 0x40)]
        [InlineData(3, 12, 13, 0x40)]
        [InlineData(5, 10, 13, 0x40)]
        public void AlignmentNotNeeded(int multiple, int loopStart, int loopEnd, int adpcmLength)
        {
            var alignment = new GcAdpcmAlignment(multiple, loopStart, loopEnd, new byte[adpcmLength], new short[16]);
            Assert.False(alignment.AlignmentNeeded);
        }

        [Theory]
        [InlineData(2, 3, 10, 0x40)]
        [InlineData(4, 2, 10, 0x40)]
        [InlineData(3, 31, 50, 0x40)]
        [InlineData(16, 24, 50, 0x40)]
        [InlineData(16, 31, 50, 0x40)]
        public void AlignmentNeeded(int multiple, int loopStart, int loopEnd, int adpcmLength)
        {
            var alignment = new GcAdpcmAlignment(multiple, loopStart, loopEnd, new byte[adpcmLength], new short[16]);
            Assert.True(alignment.AlignmentNeeded);
        }

        [Theory]
        [InlineData(0, 0, 10, 0x40)]
        [InlineData(1, 7, 10, 0x40)]
        [InlineData(5, 10, 13, 0x40)]
        public void PassedInDataIsSet(int multiple, int loopStart, int loopEnd, int adpcmLength)
        {
            var alignment = new GcAdpcmAlignment(multiple, loopStart, loopEnd, new byte[adpcmLength], new short[16]);
            var expected = new[] { multiple, loopStart, loopEnd };
            var actual = new[] { alignment.AlignmentMultiple, alignment.LoopStart, alignment.LoopEnd };
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(2, 3, 10, 0x40, 4, 11)]
        [InlineData(4, 2, 10, 0x40, 4, 12)]
        [InlineData(3, 31, 50, 0x40, 33, 52)]
        [InlineData(16, 24, 50, 0x40, 32, 58)]
        [InlineData(16, 31, 50, 0x40, 32, 51)]
        public void AlignedLoopPointsAreCorrect(int multiple, int loopStart, int loopEnd, int adpcmLength,
            int expectedLoopStart, int expectedLoopEnd)
        {
            var alignment = new GcAdpcmAlignment(multiple, loopStart, loopEnd, new byte[adpcmLength], new short[16]);
            var expected = new[] { expectedLoopStart, expectedLoopEnd };
            var actual = new[] { alignment.LoopStartAligned, alignment.SampleCountAligned };
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(1000, 4524, 100, 2)]
        [InlineData(1000, 2012, 1, 2)]
        [InlineData(1000, 60, 1, 2)]
        [InlineData(1000, 60, 20, 2)]
        public void AlignedAdpcmIsCorrect(int multiple, int loopStart, int sineCycles, int tolerance)
        {
            int loopEnd = sineCycles * 4 * SamplesPerFrame + loopStart;
            var pcm = GenerateSineWave(GetNextMultiple(loopEnd, SamplesPerFrame), 1, SamplesPerFrame * 4);
            var coefs = GcAdpcmCoefficients.CalculateCoefficients(pcm);
            var adpcm = GcAdpcmEncoder.Encode(pcm, coefs);
            var alignment = new GcAdpcmAlignment(multiple, loopStart, loopEnd, adpcm, coefs);
            var pcmAligned = GcAdpcmDecoder.Decode(alignment.AdpcmAligned, coefs, new GcAdpcmParameters { SampleCount = alignment.SampleCountAligned });
            var pcmExpected = GenerateSineWave(alignment.SampleCountAligned, 1, SamplesPerFrame * 4);

            var diff = new double[alignment.SampleCountAligned];

            //Skip the first sine cycle and last ADPCM frame due to history samples
            int end = GetNextMultiple(alignment.SampleCountAligned, SamplesPerFrame) - SamplesPerFrame;
            for (int i = SamplesPerFrame * 4; i < end; i++)
            {
                double dist = Math.Abs(pcmExpected[i] - pcmAligned[i]);
                diff[i] = dist;
            }

            Assert.All(diff, x => Assert.InRange(x, 0, tolerance));
        }

        [Theory]
        [InlineData(1000, 4524, 100)]
        [InlineData(1000, 2012, 1)]
        [InlineData(1000, 60, 1)]
        [InlineData(1000, 60, 20)]
        public void AlignedPcmIsCorrect(int multiple, int loopStart, int sineCycles)
        {
            int loopEnd = sineCycles * 4 * SamplesPerFrame + loopStart;
            var pcm = GenerateSineWave(GetNextMultiple(loopEnd, SamplesPerFrame), 1, SamplesPerFrame * 4);
            var coefs = GcAdpcmCoefficients.CalculateCoefficients(pcm);
            var adpcm = GcAdpcmEncoder.Encode(pcm, coefs);
            var alignment = new GcAdpcmAlignment(multiple, loopStart, loopEnd, adpcm, coefs);
            var pcmAligned = alignment.PcmAligned;
            var pcmExpected = GcAdpcmDecoder.Decode(alignment.AdpcmAligned, coefs, new GcAdpcmParameters { SampleCount = alignment.SampleCountAligned });

            Assert.Equal(pcmExpected, pcmAligned);
        }
    }
}
