using System;
using System.Linq;
using VGAudio.Formats;
using VGAudio.Formats.GcAdpcm;
using VGAudio.Tests.Equality;
using VGAudio.Utilities;
using Xunit;

namespace VGAudio.Tests.Formats
{
    public class GcAdpcmFormatTests
    {
        [Theory]
        [InlineData(100, true, 30, 90)]
        [InlineData(100, false, 0, 0)]
        public void LoopsProperlyAfterEncoding(int sampleCount, bool looping, int loopStart, int loopEnd)
        {
            Pcm16Format pcm = GenerateAudio.GeneratePcmSineWave(sampleCount, 1, 48000)
                .WithLoop(looping, loopStart, loopEnd);

            IAudioFormat adpcm = new GcAdpcmFormat().EncodeFromPcm16(pcm);
            Assert.Equal(looping, adpcm.Looping);
            Assert.Equal(loopStart, adpcm.LoopStart);
            Assert.Equal(loopEnd, adpcm.LoopEnd);
        }

        [Theory]
        [InlineData(100, true, 30, 90)]
        [InlineData(100, false, 0, 0)]
        public void LoopsProperlyAfterDecoding(int sampleCount, bool looping, int loopStart, int loopEnd)
        {
            GcAdpcmFormat adpcm = GenerateAudio.GenerateAdpcmEmpty(sampleCount, 1, 48000)
                .WithLoop(looping, loopStart, loopEnd);

            IAudioFormat pcm = adpcm.ToPcm16();
            Assert.Equal(looping, pcm.Looping);
            Assert.Equal(loopStart, pcm.LoopStart);
            Assert.Equal(loopEnd, pcm.LoopEnd);
        }

        [Fact]
        public void AddingFormatsTogether()
        {
            GcAdpcmChannel[] channels = GenerateAudio.GenerateAdpcmSineWave(100, 2, 48000).Channels;

            GcAdpcmFormat adpcm = new GcAdpcmFormat(new[] { channels[0] }, 48000);
            GcAdpcmFormat adpcm2 = new GcAdpcmFormat(new[] { channels[1] }, 48000);
            GcAdpcmFormat combined = adpcm.Add(adpcm2);

            Assert.Equal(adpcm.Channels[0], combined.Channels[0], new GcAdpcmChannelComparer());
            Assert.Equal(adpcm2.Channels[0], combined.Channels[1], new GcAdpcmChannelComparer());
        }

        [Fact]
        public void AddingFormatsOfDifferentLengthThrows()
        {
            GcAdpcmChannel[] channels = GenerateAudio.GenerateAdpcmSineWave(100, 2, 48000).Channels;
            GcAdpcmChannel[] channels2 = GenerateAudio.GenerateAdpcmSineWave(200, 2, 48000).Channels;
            GcAdpcmFormat adpcm = new GcAdpcmFormat(new[] { channels[0] }, 48000);
            GcAdpcmFormat adpcm2 = new GcAdpcmFormat(new[] { channels2[1] }, 48000);

            Exception ex = Record.Exception(() => adpcm.Add(adpcm2));
            Assert.IsType<ArgumentException>(ex);
        }

        [Fact]
        public void GettingSpecificChannels()
        {
            GcAdpcmFormat adpcm = GenerateAudio.GenerateAdpcmSineWave(100, 4, 48000);
            GcAdpcmFormat partial = adpcm.GetChannels(2, 0, 2);

            Assert.Equal(100, partial.SampleCount);
            Assert.Equal(3, partial.ChannelCount);
            Assert.Equal(adpcm.Channels[2], partial.Channels[0], new GcAdpcmChannelComparer());
            Assert.Equal(adpcm.Channels[0], partial.Channels[1], new GcAdpcmChannelComparer());
            Assert.Equal(adpcm.Channels[2], partial.Channels[2], new GcAdpcmChannelComparer());
        }

        [Fact]
        public void GettingChannelsThrowsWhenOutOfBounds()
        {
            GcAdpcmFormat adpcm = GenerateAudio.GenerateAdpcmSineWave(100, 4, 48000);

            Exception ex = Record.Exception(() => adpcm.GetChannels(4, 0));
            Assert.IsType<ArgumentException>(ex);
        }

        [Theory]
        [InlineData(Endianness.BigEndian)]
        [InlineData(Endianness.LittleEndian)]
        public void BuildSeekTableOneChannel(Endianness endianness)
        {
            short[] expected = { 0, 0, 50, 49, 100, 99 };
            GcAdpcmFormat adpcm = GetAccendingAdpcm(50, 112, 0);

            Assert.Equal(expected.ToByteArray(endianness), adpcm.BuildSeekTable(3, endianness));
        }

        [Theory]
        [InlineData(Endianness.BigEndian)]
        [InlineData(Endianness.LittleEndian)]
        public void BuildSeekTableTwoChannels(Endianness endianness)
        {
            short[] expected = { 0, 0, 0, 0, 50, 49, 100, 99, 100, 99, 150, 149 };
            GcAdpcmFormat adpcm = GetAccendingAdpcm(50, 112, 0, 50);

            Assert.Equal(expected.ToByteArray(endianness), adpcm.BuildSeekTable(3, endianness));
        }

        [Theory]
        [InlineData(Endianness.BigEndian)]
        [InlineData(Endianness.LittleEndian)]
        public void BuildSeekTableFourChannels(Endianness endianness)
        {
            short[] expected =
            {
                0, 0, 0, 0, 0, 0, 0, 0,
                50, 49, 100, 99, 250, 249, 150, 149,
                100, 99, 150, 149, 300, 299, 200, 199
            };
            GcAdpcmFormat adpcm = GetAccendingAdpcm(50, 112, 0, 50, 200, 100);

            Assert.Equal(expected.ToByteArray(endianness), adpcm.BuildSeekTable(3, endianness));
        }

        [Theory]
        [InlineData(Endianness.BigEndian)]
        [InlineData(Endianness.LittleEndian)]
        public void BuildSeekTableFourChannelsTruncated(Endianness endianness)
        {
            short[] expected =
            {
                0, 0, 0, 0, 0, 0, 0, 0,
                50, 49, 100, 99, 250, 249, 150, 149
            };
            GcAdpcmFormat adpcm = GetAccendingAdpcm(50, 112, 0, 50, 200, 100);

            Assert.Equal(expected.ToByteArray(endianness), adpcm.BuildSeekTable(2, endianness));
        }

        private GcAdpcmFormat GetAccendingAdpcm(int samplesPerEntry, int count, params int[] starts)
        {
            short[][] pcm = starts.Select(start => GenerateAudio.GenerateAccendingShorts(start, count)).ToArray();
            Pcm16Format pcmFormat = Pcm16Format.GetBuilder(pcm, 48000).Build();
            GcAdpcmFormat adpcm = new GcAdpcmFormat().EncodeFromPcm16(pcmFormat);

            for (int i = 0; i < adpcm.ChannelCount; i++)
            {
                adpcm.Channels[i] = adpcm.Channels[i]
                    .GetCloneBuilder()
                    .WithSamplesPerSeekTableEntry(samplesPerEntry)
                    .Build();
            }

            return adpcm;
        }
    }
}
