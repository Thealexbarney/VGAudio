using VGAudio.Formats;
using Xunit;

namespace VGAudio.Tests.Formats
{
    public class GcAdpcmFormatBuilderTests
    {
        [Fact]
        public void ChannelsAreSetAfterCreation()
        {
            var channels = GenerateAudio.GenerateAdpcmChannelsEmpty(100, 2);
            var adpcm = GcAdpcmFormat.GetBuilder(channels).Build();
            Assert.Same(channels, adpcm.Channels);
        }
    }
}
