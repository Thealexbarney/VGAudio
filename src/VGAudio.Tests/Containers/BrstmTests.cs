using VGAudio.Containers;
using VGAudio.Containers.Bxstm;
using VGAudio.Formats;
using Xunit;

namespace VGAudio.Tests.Containers
{
    public class BrstmTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(8)]
        public void BrstmBuildAndParseEqual(int numChannels)
        {
            GcAdpcmFormat audio = GenerateAudio.GenerateAdpcmSineWave(BuildParseTestOptions.Samples, numChannels, BuildParseTestOptions.SampleRate);

            BuildParseTests.BuildParseCompareAudio(audio, new BrstmWriter(), new BrstmReader());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(8)]
        public void BrstmBuildAndParseEqualPcm16(int numChannels)
        {
            Pcm16Format audio = GenerateAudio.GeneratePcmSineWave(BuildParseTestOptions.Samples, numChannels, BuildParseTestOptions.SampleRate);
            var writer = new BrstmWriter {Configuration = {Codec = BxstmCodec.Pcm16Bit}};

            BuildParseTests.BuildParseCompareAudio(audio, writer, new BrstmReader());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(8)]
        public void BrstmBuildAndParseEqualPcm8(int numChannels)
        {
            Pcm8SignedFormat audio = GenerateAudio.GeneratePcm8SignedSineWave(BuildParseTestOptions.Samples, numChannels, BuildParseTestOptions.SampleRate);
            var writer = new BrstmWriter {Configuration = {Codec = BxstmCodec.Pcm8Bit}};

            BuildParseTests.BuildParseCompareAudio(audio, writer, new BrstmReader());
        }

        [Fact]
        public void BrstmLoopAlignmentIsSet()
        {
            GcAdpcmFormat audio = GenerateAudio.GenerateAdpcmSineWave(BuildParseTestOptions.Samples, 1, BuildParseTestOptions.SampleRate);
            audio = audio.WithLoop(true, 1288, 16288);
            var writer = new BrstmWriter {Configuration = {LoopPointAlignment = 700}};

            byte[] builtFile = writer.GetFile(audio);
            IAudioFormat parsedAudio = new BrstmReader().ReadFormat(builtFile);

            Assert.Equal(1400, parsedAudio.LoopStart);
            Assert.Equal(16400, parsedAudio.LoopEnd);
        }
    }
}
