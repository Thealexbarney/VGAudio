using System.Collections.Generic;
using VGAudio.Formats;
using VGAudio.Formats.GcAdpcm;
using Xunit;

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
            var tracks = new List<GcAdpcmTrack> { new GcAdpcmTrack(1, 0, 0), new GcAdpcmTrack(1, 1, 0) };
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
    }
}
