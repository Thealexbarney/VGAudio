using VGAudio.Codecs.GcAdpcm;
using VGAudio.Formats.GcAdpcm;
using Xunit;
// ReSharper disable RedundantArgumentDefaultValue

namespace VGAudio.Tests.Formats.GcAdpcm
{
    public class GcAdpcmChannelTests
    {
        [Fact]
        public void ReturnsSameDataAfterCreation()
        {
            var adpcm = new byte[GcAdpcmMath.SampleCountToByteCount(100)];
            var coefs = new short[16];
            GcAdpcmChannel channel = new GcAdpcmChannelBuilder(adpcm, coefs, 100).Build();

            Assert.Equal(100, channel.SampleCount);
            Assert.Same(adpcm, channel.Adpcm);
            Assert.Same(coefs, channel.Coefs);
        }

        [Fact]
        public void ReturnsEmptyArrayWhenNoSeekTable()
        {
            GcAdpcmChannel channel = GetBuilder().Build();
            Assert.Empty(channel.GetSeekTable());
        }

        [Fact]
        public void ReturnsSameSeekTableAsAssigned()
        {
            var seekTable = new short[100];
            GcAdpcmChannel channel = GetBuilder().WithSeekTable(seekTable, 10).Build();
            Assert.Same(seekTable, channel.GetSeekTable());
        }

        [Fact]
        public void PcmIsSetWhenLoopPointsNeedAlignment()
        {
            //A new pcm array is generated if it doesn't exist, so check that the array returned
            //is the same when calling multiple times
            GcAdpcmChannel channel = GetBuilder().WithLoop(true, 20, 40).WithLoopAlignment(15).WithLoopContext(20, 0, 0, 0).Build();
            Assert.Same(channel.GetPcmAudio(), channel.GetPcmAudio());
        }

        [Fact]
        public void PcmIsNotSetWhenSeekTableAndLoopContextNotNeeded()
        {
            GcAdpcmChannel channel = GetBuilder().WithLoop(true, 20, 40).WithLoopContext(20, 0, 0, 0).Build();
            Assert.NotSame(channel.GetPcmAudio(), channel.GetPcmAudio());
        }

        [Fact]
        public void PcmIsSameAfterRebuildingChannel()
        {
            GcAdpcmChannel channel = GetBuilder().WithLoop(true, 10, 100).Build();
            GcAdpcmChannel channel2 = channel.GetCloneBuilder().Build();
            Assert.Same(channel.GetPcmAudio(), channel2.GetPcmAudio());
        }

        [Fact]
        public void PcmIsSameAfterChangingNonCriticalParametersAndRebuildingChannel()
        {
            GcAdpcmChannel channel = GetBuilder().WithLoop(true, 10, 100).Build();
            GcAdpcmChannel channel2 = channel.GetCloneBuilder().WithLoop(true, 20, 100).WithSamplesPerSeekTableEntry(20).Build();
            Assert.Same(channel.GetPcmAudio(), channel2.GetPcmAudio());
        }

        [Fact]
        public void PcmIsNotSameAfterChangingAndRebuildingChannel()
        {
            GcAdpcmChannel channel = GetBuilder().WithLoop(true, 10, 100).Build();
            GcAdpcmChannel channel2 = channel.GetCloneBuilder().WithLoopAlignment(15).Build();
            Assert.NotSame(channel.GetPcmAudio(), channel2.GetPcmAudio());
        }

        [Fact]
        public void PcmLengthIsCorrectAfterBuilding()
        {
            GcAdpcmChannel channel = GetBuilder(100).WithLoop(true, 10, 100).Build();
            Assert.Equal(100, channel.GetPcmAudio().Length);
        }

        [Fact]
        public void PcmLengthIsCorrectAfterAlignment()
        {
            GcAdpcmChannel channel = GetBuilder(100).WithLoop(true, 10, 100).WithLoopAlignment(15).Build();
            Assert.Equal(105, channel.GetPcmAudio().Length);
        }

        [Fact]
        public void PcmLengthIsCorrectAfterAlignmentAndUnalignment()
        {
            GcAdpcmChannel channel = GetBuilder(100).WithLoop(true, 10, 100).WithLoopAlignment(15).Build();
            GcAdpcmChannel channel2 = channel.GetCloneBuilder().WithLoopAlignment(0).Build();
            Assert.Equal(105, channel.GetPcmAudio().Length);
            Assert.Equal(100, channel2.GetPcmAudio().Length);
        }

        [Fact]
        public void AdpcmLengthIsCorrectAfterBuilding()
        {
            GcAdpcmChannel channel = GetBuilder(100).WithLoop(true, 10, 100).Build();
            Assert.Equal(GcAdpcmMath.SampleCountToByteCount(100), channel.GetAdpcmAudio().Length);
        }

        [Fact]
        public void AdpcmLengthIsCorrectAfterAlignment()
        {
            GcAdpcmChannel channel = GetBuilder(100).WithLoop(true, 10, 100).WithLoopAlignment(15).Build();
            Assert.Equal(GcAdpcmMath.SampleCountToByteCount(105), channel.GetAdpcmAudio().Length);
        }

        [Fact]
        public void AdpcmLengthIsCorrectAfterRealignment()
        {
            GcAdpcmChannel channel = GetBuilder(100).WithLoop(true, 10, 100).WithLoopAlignment(15).Build();
            GcAdpcmChannel channel2 = channel.GetCloneBuilder().WithLoopAlignment(20).Build();
            Assert.Equal(GcAdpcmMath.SampleCountToByteCount(110), channel2.GetAdpcmAudio().Length);
        }

        [Fact]
        public void AdpcmIsSameAfterAligningAndRebuilding()
        {
            GcAdpcmChannel channel = GetBuilder(100).WithLoop(true, 10, 100).WithLoopAlignment(15).Build();
            GcAdpcmChannel channel2 = channel.GetCloneBuilder().WithSamplesPerSeekTableEntry(10).Build();
            Assert.Same(channel.GetAdpcmAudio(), channel2.GetAdpcmAudio());
        }

        [Fact]
        public void AdpcmIsSameAfterAligningAndUnaligning()
        {
            GcAdpcmChannel channel = GetBuilder(100).WithLoop(true, 10, 100).Build();
            GcAdpcmChannel channel2 = channel.GetCloneBuilder().WithLoopAlignment(15).Build();
            GcAdpcmChannel channel3 = channel2.GetCloneBuilder().WithLoopAlignment(0).Build();
            Assert.Same(channel.GetAdpcmAudio(), channel3.GetAdpcmAudio());
            Assert.NotSame(channel.GetAdpcmAudio(), channel2.GetAdpcmAudio());
        }

        [Fact]
        public void AdpcmIsNotSameAfterAligningAndRealigning()
        {
            GcAdpcmChannel channel = GetBuilder().WithLoop(true, 10, 100).WithLoopAlignment(15).Build();
            GcAdpcmChannel channel2 = channel.GetCloneBuilder().WithLoopAlignment(20).Build();
            Assert.NotSame(channel.GetAdpcmAudio(), channel2.GetAdpcmAudio());
        }

        [Fact]
        public void SeekTableIsSameAfterRebuilding()
        {
            GcAdpcmChannel channel = GetBuilder().WithSamplesPerSeekTableEntry(10).Build();
            GcAdpcmChannel channel2 = channel.GetCloneBuilder().WithLoop(true, 10, 100).Build();
            Assert.Same(channel.GetSeekTable(), channel2.GetSeekTable());
        }
        
        private static GcAdpcmChannelBuilder GetBuilder(int sampleCount = 100)
        {
            var adpcm = new byte[GcAdpcmMath.SampleCountToByteCount(sampleCount)];
            var coefs = new short[16];
            return new GcAdpcmChannelBuilder(adpcm, coefs, sampleCount);
        }
    }
}
