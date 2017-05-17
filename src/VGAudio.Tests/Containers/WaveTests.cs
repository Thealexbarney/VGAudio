using VGAudio.Containers;
using VGAudio.Containers.Wave;
using VGAudio.Formats;
using Xunit;

namespace VGAudio.Tests.Containers
{
    public class WaveTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(8)]
        public void WavePcm16BuildAndParseEqual(int numChannels)
        {
            Pcm16Format audio = GenerateAudio.GeneratePcmSineWave(BuildParseTestOptions.Samples, numChannels, BuildParseTestOptions.SampleRate);

            BuildParseTests.BuildParseCompareAudio(audio, new WaveWriter(), new WaveReader());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(8)]
        public void WavePcm8BuildAndParseEqual(int numChannels)
        {
            Pcm8Format audio = GenerateAudio.GeneratePcm8SineWave(BuildParseTestOptions.Samples, numChannels, BuildParseTestOptions.SampleRate);
            var writer = new WaveWriter { Configuration = { Codec = WaveCodec.Pcm8Bit } };

            BuildParseTests.BuildParseCompareAudio(audio, writer, new WaveReader());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(8)]
        public void WavePcm16LoopedBuildAndParseEqual(int numChannels)
        {
            Pcm16Format audio = GenerateAudio.GeneratePcmSineWave(BuildParseTestOptions.Samples, numChannels, BuildParseTestOptions.SampleRate).WithLoop(true);

            BuildParseTests.BuildParseCompareAudio(audio, new WaveWriter(), new WaveReader());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(8)]
        public void WavePcm8LoopedBuildAndParseEqual(int numChannels)
        {
            Pcm8Format audio = GenerateAudio.GeneratePcm8SineWave(BuildParseTestOptions.Samples, numChannels, BuildParseTestOptions.SampleRate).WithLoop(true);
            var writer = new WaveWriter { Configuration = { Codec = WaveCodec.Pcm8Bit } };

            BuildParseTests.BuildParseCompareAudio(audio, writer, new WaveReader());
        }
    }
}
