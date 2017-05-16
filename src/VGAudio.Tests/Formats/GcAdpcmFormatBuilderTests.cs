using System.Collections.Generic;
using VGAudio.Formats;
using VGAudio.Formats.GcAdpcm;
using Xunit;
using static VGAudio.Formats.GcAdpcm.GcAdpcmHelpers;

namespace VGAudio.Tests.Formats
{
    public class GcAdpcmFormatBuilderTests
    {
        [Fact]
        public void ChannelsAreSetAfterCreation()
        {
            GcAdpcmChannel[] channels = GenerateAudio.GenerateAdpcmChannelsEmpty(100, 2);
            GcAdpcmFormat adpcm = GcAdpcmFormat.GetBuilder(channels, 32000).Build();
            Assert.Equal(channels[0].Adpcm, adpcm.Channels[0].Adpcm);
            Assert.Equal(channels[1].Adpcm, adpcm.Channels[1].Adpcm);
        }

        [Fact]
        public void TracksAreSetAfterCreation()
        {
            GcAdpcmChannel[] channels = GenerateAudio.GenerateAdpcmChannelsEmpty(100, 2);
            var tracks = new List<AudioTrack> { new AudioTrack(1, 0, 0), new AudioTrack(1, 1, 0) };
            GcAdpcmFormat adpcm = GcAdpcmFormat.GetBuilder(channels, 32000).WithTracks(tracks).Build();
            Assert.Equal(tracks, adpcm.Tracks);
        }

        [Fact]
        public void AlignmentAlignsChildChannels()
        {
            GcAdpcmChannel[] channels = GenerateAudio.GenerateAdpcmChannelsEmpty(100, 2);
            GcAdpcmFormat adpcm = GcAdpcmFormat.GetBuilder(channels, 32000).WithAlignment(15).Loop(true, 10, 100).Build();
            Assert.All(adpcm.Channels, x => Assert.Equal(100, x.UnalignedSampleCount));
            Assert.All(adpcm.Channels, x => Assert.Equal(105, x.SampleCount));
        }

        [Fact]
        public void ReAlignmentAlignsChildChannels()
        {
            GcAdpcmChannel[] channels = GenerateAudio.GenerateAdpcmChannelsEmpty(100, 2);
            GcAdpcmFormat adpcm = GcAdpcmFormat.GetBuilder(channels, 32000).WithAlignment(15).Loop(true, 10, 100).Build();
            GcAdpcmFormat adpcm2 = adpcm.GetCloneBuilder().WithAlignment(7).Build();
            Assert.All(adpcm2.Channels, x => Assert.Equal(100, x.UnalignedSampleCount));
            Assert.All(adpcm2.Channels, x => Assert.Equal(104, x.SampleCount));
        }

        [Fact]
        public void CloningAndModifyingDoesNotChangeFirstFormat()
        {
            GcAdpcmChannel[] channels = GenerateAudio.GenerateAdpcmChannelsEmpty(100, 2);
            GcAdpcmFormat adpcm = GcAdpcmFormat.GetBuilder(channels, 32000).WithAlignment(15).Loop(true, 10, 100).Build();
            GcAdpcmFormat unused = adpcm.GetCloneBuilder().WithAlignment(7).Build();
            Assert.All(adpcm.Channels, x => Assert.Equal(100, x.UnalignedSampleCount));
            Assert.All(adpcm.Channels, x => Assert.Equal(105, x.SampleCount));
        }

        [Theory]
        [InlineData(100, true, 30, 90)]
        public void AdpcmDataLengthIsCorrectAfterLooping(int sampleCount, bool looping, int loopStart, int loopEnd)
        {
            GcAdpcmFormat adpcm = GenerateAudio.GenerateAdpcmEmpty(sampleCount, 1, 48000)
                .WithLoop(looping, loopStart, loopEnd);

            Assert.Equal(SampleCountToByteCount(sampleCount), adpcm.Channels[0].GetAdpcmAudio().Length);
        }

        [Theory]
        [InlineData(100, true, 30, 90, 50)]
        public void AdpcmDataLengthIsCorrectAfterUnlooping(int sampleCount, bool looping, int loopStart, int loopEnd, int alignment)
        {
            GcAdpcmFormat adpcm = GenerateAudio.GenerateAdpcmEmpty(sampleCount, 1, 48000)
                .WithAlignment(alignment)
                .WithLoop(looping, loopStart, loopEnd)
                .WithLoop(false);

            Assert.Equal(SampleCountToByteCount(sampleCount), adpcm.Channels[0].GetAdpcmAudio().Length);
        }

        [Theory]
        [InlineData(100, true, 30, 90, 50)]
        public void AdpcmDataLengthIsCorrectAfterAlignment(int sampleCount, bool looping, int loopStart, int loopEnd, int alignment)
        {
            GcAdpcmFormat adpcm = GenerateAudio.GenerateAdpcmEmpty(sampleCount, 1, 48000)
                .WithAlignment(alignment)
                .WithLoop(looping, loopStart, loopEnd);

            int extraSamples = Utilities.Helpers.GetNextMultiple(loopStart, alignment) - loopStart;

            Assert.Equal(SampleCountToByteCount(loopEnd + extraSamples), adpcm.Channels[0].GetAdpcmAudio().Length);
        }

        [Theory]
        [InlineData(100, true, 30, 90, 50)]
        public void AdpcmDataLengthIsCorrectAfterUnalignment(int sampleCount, bool looping, int loopStart, int loopEnd, int alignment)
        {
            GcAdpcmFormat adpcm = GenerateAudio.GenerateAdpcmEmpty(sampleCount, 1, 48000)
                .WithAlignment(alignment)
                .WithLoop(looping, loopStart, loopEnd)
                .WithAlignment(0);

            Assert.Equal(SampleCountToByteCount(sampleCount), adpcm.Channels[0].GetAdpcmAudio().Length);
        }

        [Theory]
        [InlineData(100, true, 30, 90, 50)]
        public void AdpcmLoopIsCorrectAfterAlignment(int sampleCount, bool looping, int loopStart, int loopEnd, int alignment)
        {
            IAudioFormat adpcm = GenerateAudio.GenerateAdpcmEmpty(sampleCount, 1, 48000)
                .WithAlignment(alignment)
                .WithLoop(looping, loopStart, loopEnd);

            int extraSamples = Utilities.Helpers.GetNextMultiple(loopStart, alignment) - loopStart;

            Assert.Equal(loopStart + extraSamples, adpcm.LoopStart);
            Assert.Equal(loopEnd + extraSamples, adpcm.LoopEnd);
        }

        [Theory]
        [InlineData(100, true, 30, 90, 50)]
        public void AdpcmLoopIsCorrectAfterUnalignment(int sampleCount, bool looping, int loopStart, int loopEnd, int alignment)
        {
            IAudioFormat adpcm = GenerateAudio.GenerateAdpcmEmpty(sampleCount, 1, 48000)
                .WithAlignment(alignment)
                .WithLoop(looping, loopStart, loopEnd)
                .WithAlignment(0);

            Assert.Equal(loopStart, adpcm.LoopStart);
            Assert.Equal(loopEnd, adpcm.LoopEnd);
        }

        [Theory]
        [InlineData(100, true, 30, 90)]
        public void AdpcmSampleCountIsCorrectAfterLooping(int sampleCount, bool looping, int loopStart, int loopEnd)
        {
            IAudioFormat adpcm = GenerateAudio.GenerateAdpcmEmpty(sampleCount, 1, 48000)
                .WithLoop(looping, loopStart, loopEnd);

            Assert.Equal(sampleCount, adpcm.SampleCount);
        }
    }
}
