using VGAudio.Codecs.GcAdpcm;
using VGAudio.Formats.GcAdpcm;
using Xunit;

namespace VGAudio.Tests.Formats.GcAdpcm
{
    public class GcAdpcmChannelBuilderTests
    {
        [Theory]
        [InlineData(100)]
        public void ConstructorAssignmentWorks(int sampleCount)
        {
            var adpcm = new byte[GcAdpcmMath.SampleCountToByteCount(sampleCount)];
            var coefs = new short[16];
            var builder = new GcAdpcmChannelBuilder(adpcm, coefs, sampleCount);
            Assert.Equal(adpcm, builder.Adpcm);
            Assert.Equal(coefs, builder.Coefs);
            Assert.Equal(sampleCount, builder.SampleCount);
        }

        [Theory]
        [InlineData(100, 10)]
        public void SetSeekTablePrebuilt(int sampleCount, int samplesPerEntry)
        {
            var seekTable = new short[100];
            var builder = GetBuilder(sampleCount).WithSeekTable(seekTable, samplesPerEntry);
            Assert.Equal(seekTable, builder.SeekTable);
            Assert.Equal(samplesPerEntry, builder.SamplesPerSeekTableEntry);
            Assert.False(builder.SeekTableIsSelfCalculated);
        }

        [Theory]
        [InlineData(100, 10, true)]
        [InlineData(100, 10, false)]
        public void SetSeekTablePrebuiltSelfCalculated(int sampleCount, int samplesPerEntry, bool isSelfCalculated)
        {
            var seekTable = new short[100];
            var builder = GetBuilder(sampleCount).WithSeekTable(seekTable, samplesPerEntry, isSelfCalculated);
            Assert.Equal(seekTable, builder.SeekTable);
            Assert.Equal(samplesPerEntry, builder.SamplesPerSeekTableEntry);
            Assert.Equal(isSelfCalculated, builder.SeekTableIsSelfCalculated);
        }

        [Fact]
        public void SetSeekTableSamplesOnlyReplacesOldTable()
        {
            var builder = GetBuilder().WithSeekTable(new short[100], 100, true);
            builder.WithSamplesPerSeekTableEntry(10);
            Assert.Null(builder.SeekTable);
            Assert.False(builder.SeekTableIsSelfCalculated);
            Assert.Equal(10, builder.SamplesPerSeekTableEntry);
        }

        [Fact]
        public void SetSeekTableSamplesDoesntReplaceOldTable()
        {
            var seekTable = new short[100];
            var builder = GetBuilder().WithSeekTable(seekTable, 100, true);
            builder.WithSamplesPerSeekTableEntry(100);
            Assert.Equal(seekTable, builder.SeekTable);
            Assert.True(builder.SeekTableIsSelfCalculated);
            Assert.Equal(100, builder.SamplesPerSeekTableEntry);
        }

        [Theory]
        [InlineData(10, 20, 30, 40, true)]
        [InlineData(4, 14, 24, 34, false)]
        public void SetLoopContextAssignsVariables(int loopStart, short predScale, short loopHist1, short loopHist2, bool isSelfCalculated)
        {
            var builder = GetBuilder().WithLoopContext(loopStart, predScale, loopHist1, loopHist2, isSelfCalculated);
            Assert.Equal(loopStart, builder.LoopContextStart);
            Assert.Equal(predScale, builder.LoopContext.PredScale);
            Assert.Equal(loopHist1, builder.LoopContext.Hist1);
            Assert.Equal(loopHist2, builder.LoopContext.Hist2);
            Assert.Equal(isSelfCalculated, builder.LoopContextIsSelfCalculated);
        }

        [Fact]
        public void SetLoopAssignsProperlyWhenLooping()
        {
            var builder = GetBuilder().WithLoop(true, 30, 50);
            Assert.True(builder.Looping);
            Assert.Equal(30, builder.LoopStart);
            Assert.Equal(50, builder.LoopEnd);
        }

        [Fact]
        public void SetLoopAssignsProperlyWhenNotLooping()
        {
            var builder = GetBuilder().WithLoop(false, 30, 50);
            Assert.False(builder.Looping);
            Assert.Equal(0, builder.LoopStart);
            Assert.Equal(0, builder.LoopEnd);
        }

        [Fact]
        public void SetLoopAssignsProperlyBoolOnlyWhenLooping()
        {
            var builder = GetBuilder(200).WithLoop(true);
            Assert.True(builder.Looping);
            Assert.Equal(0, builder.LoopStart);
            Assert.Equal(200, builder.LoopEnd);
        }

        [Fact]
        public void SetLoopAssignsProperlyBoolOnlyWhenNotLooping()
        {
            var builder = GetBuilder().WithLoop(false);
            Assert.False(builder.Looping);
            Assert.Equal(0, builder.LoopStart);
            Assert.Equal(0, builder.LoopEnd);
        }

        [Fact]
        public void SetPreviousAssignsProperly()
        {
            var seekTable = new GcAdpcmSeekTable(new short[10], 2);
            var loopContext = new GcAdpcmLoopContext(1, 2, 3, 4, true);
            var alignment = new GcAdpcmAlignment(0, 0, 10, new byte[10], new short[16]);
            var builder = GetBuilder().WithPrevious(seekTable, loopContext, alignment);
            Assert.Equal(seekTable, builder.PreviousSeekTable);
            Assert.Equal(loopContext, builder.PreviousLoopContext);
            Assert.Equal(alignment, builder.PreviousAlignment);
        }

        [Theory]
        [InlineData(true, 10, 20, 30, true)]
        [InlineData(false, 10, 20, 30, false)]
        [InlineData(true, 11, 20, 30, false)]
        [InlineData(true, 10, 21, 30, false)]
        [InlineData(true, 10, 20, 31, false)]
        public void PreviousAlignmentIsValidTest(bool looping, int multiple, int loopStart, int loopEnd, bool expected)
        {
            var previous = new GcAdpcmAlignment(10, 20, 30, new byte[20], new short[16]);
            var builder = GetBuilder()
                .WithPrevious(null, null, previous)
                .WithLoop(looping, loopStart, loopEnd);
            builder.LoopAlignmentMultiple = multiple;

            Assert.Equal(expected, builder.PreviousAlignmentIsValid());
        }

        [Theory]
        [InlineData(false, false, 20, true)]
        [InlineData(true, false, 20, true)]
        [InlineData(true, true, 20, true)]
        [InlineData(false, true, 20, false)]
        [InlineData(false, false, 25, false)]
        [InlineData(true, false, 25, false)]
        [InlineData(true, true, 25, false)]
        [InlineData(false, true, 25, false)]
        public void LoopContextIsValidTest(bool isSelfCalculated, bool ensureSelfCalculated, int loopStart, bool expected)
        {
            var previous = new GcAdpcmLoopContext(0, 0, 0, 20, isSelfCalculated);
            var builder = GetBuilder()
                .WithPrevious(null, previous, null)
                .WithLoopContext(20, 0, 0, 0, isSelfCalculated);
            builder.EnsureLoopContextIsSelfCalculated = ensureSelfCalculated;

            Assert.Equal(expected, builder.PreviousLoopContextIsValid(loopStart));
            Assert.Equal(expected, builder.CurrentLoopContextIsValid(loopStart));
        }

        [Theory]
        [InlineData(false, false, 20, true)]
        [InlineData(true, false, 20, true)]
        [InlineData(true, true, 20, true)]
        [InlineData(false, true, 20, false)]
        [InlineData(true, true, 25, false)]
        public void PreviousSeekTableIsValidTest(bool isSelfCalculated, bool ensureSelfCalculated, int samplesPerEntry, bool expected)
        {
            var previous = new GcAdpcmSeekTable(new short[0], 20, isSelfCalculated);
            var builder = GetBuilder()
                .WithPrevious(previous, null, null)
                .WithSamplesPerSeekTableEntry(samplesPerEntry);
            builder.EnsureSeekTableIsSelfCalculated = ensureSelfCalculated;

            Assert.Equal(expected, builder.PreviousSeekTableIsValid());
        }

        [Theory]
        [InlineData(false, false, 20, true)]
        [InlineData(true, false, 20, true)]
        [InlineData(true, true, 20, true)]
        [InlineData(false, true, 20, false)]
        [InlineData(true, true, 25, false)]
        public void CurrentSeekTableIsValidTest(bool isSelfCalculated, bool ensureSelfCalculated, int samplesPerEntry, bool expected)
        {
            var builder = GetBuilder()
                .WithSeekTable(new short[0], 20, isSelfCalculated)
                .WithSamplesPerSeekTableEntry(samplesPerEntry);
            builder.EnsureSeekTableIsSelfCalculated = ensureSelfCalculated;

            Assert.Equal(expected, builder.CurrentSeekTableIsValid());
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(10, 25, 65)]
        public void CreateAlignmentWithNoPrevious(int multiple, int loopStart, int loopEnd)
        {
            var sampleCount = 100;
            var (adpcm, coefs) = GenerateAudio.GenerateAdpcmEmpty(sampleCount);

            var builder = new GcAdpcmChannelBuilder(adpcm, coefs, sampleCount)
                .WithLoop(true, loopStart, loopEnd);
            builder.LoopAlignmentMultiple = multiple;
            var alignment = builder.GetAlignment();

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

            var builder = new GcAdpcmChannelBuilder(adpcm, coefs, sampleCount)
                .WithLoop(true, loopStart, loopEnd)
                .WithPrevious(null, null, previous);
            builder.LoopAlignmentMultiple = multiple;
            var alignment = builder.GetAlignment();

            Assert.Equal(previous, alignment);
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(10, 25, 65)]
        public void CreateAlignmentReplacePreviousWhenMultipleChanges(int multiple, int loopStart, int loopEnd)
        {
            var sampleCount = 100;
            var (adpcm, coefs) = GenerateAudio.GenerateAdpcmEmpty(sampleCount);
            var previous = new GcAdpcmAlignment(multiple, loopStart, loopEnd, adpcm, coefs);

            var builder = new GcAdpcmChannelBuilder(adpcm, coefs, sampleCount)
                .WithLoop(true, loopStart, loopEnd)
                .WithPrevious(null, null, previous);
            builder.LoopAlignmentMultiple = multiple + 1;
            var alignment = builder.GetAlignment();

            Assert.NotEqual(previous, alignment);
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(10, 25, 65)]
        public void CreateAlignmentReplacePreviousWhenLoopChanges(int multiple, int loopStart, int loopEnd)
        {
            var sampleCount = 100;
            var (adpcm, coefs) = GenerateAudio.GenerateAdpcmEmpty(sampleCount);
            var previous = new GcAdpcmAlignment(multiple, loopStart, loopEnd, adpcm, coefs);

            var builder = new GcAdpcmChannelBuilder(adpcm, coefs, sampleCount)
                .WithLoop(true, loopStart + 1, loopEnd + 1)
                .WithPrevious(null, null, previous);
            builder.LoopAlignmentMultiple = multiple;
            var alignment = builder.GetAlignment();

            Assert.NotEqual(previous, alignment);
        }

        [Theory]
        [InlineData(0, 0, 0, 20, false)]
        [InlineData(22, 33, 44, 55, true)]
        public void CreateLoopContextWithNoPrevious(short predScale, short hist1, short hist2, int loopStart, bool isSelfCalculated)
        {
            var sampleCount = 100;
            var (adpcm, coefs) = GenerateAudio.GenerateAdpcmEmpty(sampleCount);

            var builder = new GcAdpcmChannelBuilder(adpcm, coefs, sampleCount)
                .WithLoop(true, loopStart, sampleCount)
                .WithLoopContext(loopStart, predScale, hist1, hist2, isSelfCalculated);
            builder.AlignedLoopStart = loopStart;

            var context = builder.GetLoopContext();

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
        public void GetLoopContextWithPrevious(bool isSelfCalculated, bool ensureSelfCalculated)
        {
            short num = 20;
            var sampleCount = 100;
            var (adpcm, coefs) = GenerateAudio.GenerateAdpcmEmpty(sampleCount);
            var previous = new GcAdpcmLoopContext(num, num, num, num, isSelfCalculated);

            var builder = new GcAdpcmChannelBuilder(adpcm, coefs, sampleCount)
                .WithLoop(true, num, sampleCount)
                .WithPrevious(null, previous, null)
                .PrepareForBuild();

            builder.EnsureLoopContextIsSelfCalculated = ensureSelfCalculated;
            var context = builder.GetLoopContext();

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
        public void GetLoopContextReplacePrevious(short expected, int loopStart, bool isSelfCalculated, bool ensureSelfCalculated)
        {
            short num = 20;
            short numLarge = 25;
            var sampleCount = 100;
            var (adpcm, coefs) = GenerateAudio.GenerateAdpcmEmpty(sampleCount);
            var previous = new GcAdpcmLoopContext(num, num, num, num, isSelfCalculated);

            var builder = new GcAdpcmChannelBuilder(adpcm, coefs, sampleCount)
                .WithPrevious(null, previous, null)
                .WithLoop(true, loopStart, sampleCount)
                .WithLoopContext(numLarge, numLarge, numLarge, numLarge, isSelfCalculated)
                .PrepareForBuild();

            builder.EnsureLoopContextIsSelfCalculated = ensureSelfCalculated;
            var context = builder.GetLoopContext();

            Assert.Equal(expected, context.PredScale);
            Assert.Equal(expected, context.Hist1);
            Assert.Equal(expected, context.Hist2);
            Assert.Equal(loopStart, context.LoopStart);
        }

        private static GcAdpcmChannelBuilder GetBuilder(int sampleCount = 100)
        {
            var adpcm = new byte[GcAdpcmMath.SampleCountToByteCount(sampleCount)];
            var coefs = new short[16];
            return new GcAdpcmChannelBuilder(adpcm, coefs, sampleCount);
        }
    }
}
