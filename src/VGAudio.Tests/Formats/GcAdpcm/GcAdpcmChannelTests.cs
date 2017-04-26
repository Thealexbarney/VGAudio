using VGAudio.Formats.GcAdpcm;
using Xunit;

namespace VGAudio.Tests.Formats.GcAdpcm
{
    public class GcAdpcmChannelTests
    {
        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(10, 25, 65)]
        public void CreateAlignmentWithNoPrevious(int multiple, int loopStart, int loopEnd)
        {
            var sampleCount = 100;
            var (adpcm, coefs) = GenerateAudio.GenerateAdpcmEmpty(sampleCount);
            var channel = new GcAdpcmChannel(adpcm, coefs, sampleCount);

            var builder = new GcAdpcmChannelBuilder(adpcm, coefs, sampleCount)
                .SetLoop(true, loopStart, loopEnd);
            builder.LoopAlignmentMultiple = multiple;
            var alignment = channel.CreateAlignment(builder);

            Assert.Equal(multiple, alignment.AlignmentMultiple);
            Assert.Equal(loopStart, alignment.LoopStart);
            Assert.Equal(loopEnd, alignment.LoopEnd);
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(10, 25, 65)]
        public void CreateAlignmentWithPrevious(int multiple, int loopStart, int loopEnd)
        {
            var sampleCount = 100;
            var (adpcm, coefs) = GenerateAudio.GenerateAdpcmEmpty(sampleCount);
            var previous = new GcAdpcmAlignment(multiple, loopStart, loopEnd, adpcm, coefs);
            var channel = new GcAdpcmChannel(adpcm, coefs, sampleCount);

            var builder = new GcAdpcmChannelBuilder(adpcm, coefs, sampleCount)
                .SetLoop(true, loopStart, loopEnd)
                .SetPrevious(null, null, previous);
            builder.LoopAlignmentMultiple = multiple;
            var alignment = channel.CreateAlignment(builder);

            Assert.Equal(previous, alignment);
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(10, 25, 65)]
        public void CreateAlignmentReplacePrevious(int multiple, int loopStart, int loopEnd)
        {
            var sampleCount = 100;
            var (adpcm, coefs) = GenerateAudio.GenerateAdpcmEmpty(sampleCount);
            var previous = new GcAdpcmAlignment(multiple, loopStart, loopEnd, adpcm, coefs);
            var channel = new GcAdpcmChannel(adpcm, coefs, sampleCount);

            var builder = new GcAdpcmChannelBuilder(adpcm, coefs, sampleCount)
                .SetLoop(true, loopStart, loopEnd)
                .SetPrevious(null, null, previous);
            builder.LoopAlignmentMultiple = multiple + 1;
            var alignment = channel.CreateAlignment(builder);

            Assert.NotEqual(previous, alignment);
        }

        [Theory]
        [InlineData(0, 0, 0, 20, false)]
        [InlineData(22, 33, 44, 55, true)]
        public void CreateLoopContextWithNoPrevious(short predScale, short hist1, short hist2, int loopStart, bool isSelfCalculated)
        {
            var sampleCount = 100;
            short[] pcm = null;
            var (adpcm, coefs) = GenerateAudio.GenerateAdpcmEmpty(sampleCount);
            var channel = new GcAdpcmChannel(adpcm, coefs, sampleCount);

            var builder = new GcAdpcmChannelBuilder(adpcm, coefs, sampleCount)
                .SetLoop(true, loopStart, sampleCount)
                .SetLoopContext(loopStart, predScale, hist1, hist2, isSelfCalculated);
            var context = channel.CreateLoopContext(builder, ref pcm, builder.LoopStart);

            Assert.Equal(predScale, context.PredScale);
            Assert.Equal(hist1, context.Hist1);
            Assert.Equal(hist2, context.Hist2);
            Assert.Equal(loopStart, context.LoopStart);
            Assert.Equal(isSelfCalculated, context.IsSelfCalculated);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void CreateLoopContextWithPrevious(bool isSelfCalculated, bool ensureSelfCalculated)
        {
            short num = 20;
            var sampleCount = 100;
            short[] pcm = null;
            var (adpcm, coefs) = GenerateAudio.GenerateAdpcmEmpty(sampleCount);
            var previous = new GcAdpcmLoopContext(num, num, num, num, isSelfCalculated);
            var channel = new GcAdpcmChannel(adpcm, coefs, sampleCount);

            var builder = new GcAdpcmChannelBuilder(adpcm, coefs, sampleCount)
                .SetLoop(true, num, sampleCount)
                .SetPrevious(null, previous, null);
            builder.EnsureLoopContextIsSelfCalculated = ensureSelfCalculated;
            var context = channel.CreateLoopContext(builder, ref pcm, builder.LoopStart);

            Assert.Equal(previous, context);
        }

        [Theory]
        [InlineData(20, 20, false, false)]
        [InlineData(20, 20, true, false)]
        [InlineData(20, 20, true, true)]
        [InlineData(0, 20, false, true)]
        [InlineData(25, 25, false, false)]
        [InlineData(25, 25, true, false)]
        [InlineData(25, 25, true, true)]
        [InlineData(0, 25, false, true)]
        [InlineData(0, 30, false, false)]
        [InlineData(0, 30, true, false)]
        [InlineData(0, 30, true, true)]
        [InlineData(0, 30, false, true)]
        public void CreateLoopContextReplacePrevious(short expected, int loopStart, bool isSelfCalculated, bool ensureSelfCalculated)
        {
            short num = 20;
            short numLarge = 25;
            var sampleCount = 100;
            short[] pcm = null;
            var (adpcm, coefs) = GenerateAudio.GenerateAdpcmEmpty(sampleCount);
            var previous = new GcAdpcmLoopContext(num, num, num, num, isSelfCalculated);
            var channel = new GcAdpcmChannel(adpcm, coefs, sampleCount);

            var builder = new GcAdpcmChannelBuilder(adpcm, coefs, sampleCount)
                .SetPrevious(null, previous, null)
                .SetLoop(true, loopStart, sampleCount)
                .SetLoopContext(numLarge, numLarge, numLarge, numLarge, isSelfCalculated);

            builder.EnsureLoopContextIsSelfCalculated = ensureSelfCalculated;
            var context = channel.CreateLoopContext(builder, ref pcm, builder.LoopStart);

            Assert.Equal(expected, context.PredScale);
            Assert.Equal(expected, context.Hist1);
            Assert.Equal(expected, context.Hist2);
            Assert.Equal(loopStart, context.LoopStart);
        }
    }
}
