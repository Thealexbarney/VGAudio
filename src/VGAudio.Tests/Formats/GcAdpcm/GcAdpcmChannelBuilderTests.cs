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
            var adpcm = new byte[GcAdpcmHelpers.SampleCountToByteCount(sampleCount)];
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
            var builder = GetBuilder(sampleCount).SetSeekTable(seekTable, samplesPerEntry);
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
            var builder = GetBuilder(sampleCount).SetSeekTable(seekTable, samplesPerEntry, isSelfCalculated);
            Assert.Equal(seekTable, builder.SeekTable);
            Assert.Equal(samplesPerEntry, builder.SamplesPerSeekTableEntry);
            Assert.Equal(isSelfCalculated, builder.SeekTableIsSelfCalculated);
        }

        [Fact]
        public void SetSeekTableSamplesOnlyReplacesOldTable()
        {
            var builder = GetBuilder().SetSeekTable(new short[100], 100, true);
            builder.SetSeekTable(10);
            Assert.Null(builder.SeekTable);
            Assert.False(builder.SeekTableIsSelfCalculated);
            Assert.Equal(10, builder.SamplesPerSeekTableEntry);
        }

        [Fact]
        public void SetSeekTableSamplesDoesntReplaceOldTable()
        {
            var seekTable = new short[100];
            var builder = GetBuilder().SetSeekTable(seekTable, 100, true);
            builder.SetSeekTable(100);
            Assert.Equal(seekTable, builder.SeekTable);
            Assert.True(builder.SeekTableIsSelfCalculated);
            Assert.Equal(100, builder.SamplesPerSeekTableEntry);
        }

        [Theory]
        [InlineData(10, 20, 30, 40, true)]
        [InlineData(4, 14, 24, 34, false)]
        public void SetLoopContextAssignsVariables(int loopStart, short predScale, short loopHist1, short loopHist2, bool isSelfCalculated)
        {
            var builder = GetBuilder().SetLoopContext(loopStart, predScale, loopHist1, loopHist2, isSelfCalculated);
            Assert.Equal(loopStart, builder.LoopStart);
            Assert.Equal(predScale, builder.LoopPredScale);
            Assert.Equal(loopHist1, builder.LoopHist1);
            Assert.Equal(loopHist2, builder.LoopHist2);
            Assert.Equal(isSelfCalculated, builder.LoopContextIsSelfCalculated);
        }

        [Fact]
        public void SetLoopAssignsProperlyWhenLooping()
        {
            var builder = GetBuilder().SetLoop(true, 30, 50);
            Assert.True(builder.Looping);
            Assert.Equal(30, builder.LoopStart);
            Assert.Equal(50, builder.LoopEnd);
        }

        [Fact]
        public void SetLoopAssignsProperlyWhenNotLooping()
        {
            var builder = GetBuilder().SetLoop(false, 30, 50);
            Assert.False(builder.Looping);
            Assert.Equal(0, builder.LoopStart);
            Assert.Equal(0, builder.LoopEnd);
        }

        [Fact]
        public void SetLoopAssignsProperlyBoolOnlyWhenLooping()
        {
            var builder = GetBuilder(200).SetLoop(true);
            Assert.True(builder.Looping);
            Assert.Equal(0, builder.LoopStart);
            Assert.Equal(200, builder.LoopEnd);
        }

        [Fact]
        public void SetLoopAssignsProperlyBoolOnlyWhenNotLooping()
        {
            var builder = GetBuilder().SetLoop(false);
            Assert.False(builder.Looping);
            Assert.Equal(0, builder.LoopStart);
            Assert.Equal(0, builder.LoopEnd);
        }

        [Fact]
        public void SetSeekTableRemovesPreviousSeekTable()
        {
            var builder = GetBuilder()
                .SetPrevious(new GcAdpcmSeekTable(new short[10], 2), null, null)
                .SetSeekTable(new short[10], 3);
            Assert.Null(builder.PreviousSeekTable);
        }

        [Fact]
        public void SetLoopContextRemovesPreviousLoopContext()
        {
            var builder = GetBuilder()
                .SetPrevious(null, new GcAdpcmLoopContext(5, 5, 5, 5, true), null)
                .SetLoopContext(10, 10, 10, 10);
            Assert.Null(builder.PreviousLoopContext);
        }

        [Fact]
        public void SetLoopRemovesPreviousAlignment()
        {
            var builder = GetBuilder()
                .SetPrevious(null, null, new GcAdpcmAlignment(0, 0, 10, new byte[10], new short[16]))
                .SetLoop(true, 0, 10);
            Assert.Null(builder.PreviousAlignment);
        }

        [Fact]
        public void SetPreviousAssignsProperly()
        {
            var seekTable = new GcAdpcmSeekTable(new short[10], 2);
            var loopContext = new GcAdpcmLoopContext(1, 2, 3, 4, true);
            var alignment = new GcAdpcmAlignment(0, 0, 10, new byte[10], new short[16]);
            var builder = GetBuilder().SetPrevious(seekTable, loopContext, alignment);
            Assert.Equal(seekTable, builder.PreviousSeekTable);
            Assert.Equal(loopContext, builder.PreviousLoopContext);
            Assert.Equal(alignment, builder.PreviousAlignment);
        }

        private static GcAdpcmChannelBuilder GetBuilder(int sampleCount = 100)
        {
            var adpcm = new byte[GcAdpcmHelpers.SampleCountToByteCount(sampleCount)];
            var coefs = new short[16];
            return new GcAdpcmChannelBuilder(adpcm, coefs, sampleCount);
        }
    }
}
