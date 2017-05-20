using System;
using VGAudio.Formats;
using Xunit;

namespace VGAudio.Tests.Formats
{
    public class Pcm16FormatTests
    {
        [Fact]
        public void FormatCreationWorksProperly()
        {
            short[][] channels = GetChannels(100, 2);
            Pcm16Format pcm = Pcm16Format.GetBuilder(channels, 32000).WithLoop(true, 10, 80).Build();
            Assert.Equal(channels, pcm.Channels);
            Assert.True(pcm.Looping);
            Assert.Equal(10, pcm.LoopStart);
            Assert.Equal(80, pcm.LoopEnd);
        }

        [Fact]
        public void Decoding()
        {
            short[][] channels = GetChannels(100, 2);
            Pcm16Format pcm = Pcm16Format.GetBuilder(channels, 32000).Build();
            Pcm16Format pcm2 = pcm.ToPcm16();
            Assert.NotSame(pcm, pcm2);
            Assert.Equal(pcm.Channels, pcm2.Channels);
        }

        [Fact]
        public void AddingFormatsTogether()
        {
            short[][] channels = GetChannels(100, 2);
            Pcm16Format pcm = Pcm16Format.GetBuilder(new[] { channels[0] }, 32000).Build();
            Pcm16Format pcm2 = Pcm16Format.GetBuilder(new[] { channels[1] }, 32000).Build();
            Pcm16Format combined = pcm.Add(pcm2);

            Assert.Equal(channels, combined.Channels);
        }

        [Fact]
        public void AddingFormatsOfDifferentLengthThrows()
        {
            short[][] channels = GetChannels(100, 2);
            short[][] channels2 = GetChannels(200, 2);
            Pcm16Format pcm = Pcm16Format.GetBuilder(channels, 32000).Build();
            Pcm16Format pcm2 = Pcm16Format.GetBuilder(channels2, 32000).Build();

            Exception ex = Record.Exception(() => pcm.Add(pcm2));
            Assert.IsType<ArgumentException>(ex);
        }

        [Fact]
        public void GettingSpecificChannels()
        {
            short[][] channels = GetChannels(100, 4);
            Pcm16Format pcm = Pcm16Format.GetBuilder(channels, 32000).Build();
            Pcm16Format partial = pcm.GetChannels(2, 0, 2);

            Assert.Equal(100, partial.SampleCount);
            Assert.Equal(3, partial.ChannelCount);
            Assert.Equal(channels[2], partial.Channels[0]);
            Assert.Equal(channels[0], partial.Channels[1]);
            Assert.Equal(channels[2], partial.Channels[2]);
        }

        [Fact]
        public void GettingChannelsThrowsWhenOutOfBounds()
        {
            short[][] channels = GetChannels(100, 4);
            Pcm16Format pcm = Pcm16Format.GetBuilder(channels, 32000).Build();

            Exception ex = Record.Exception(() => pcm.GetChannels(4, 0));
            Assert.IsType<ArgumentException>(ex);
        }

        private short[][] GetChannels(int sampleCount, int channelCount) =>
            GenerateAudio.GeneratePcmSineWaveChannels(sampleCount, channelCount, 32000);
    }
}
