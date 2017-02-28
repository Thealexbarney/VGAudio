using DspAdpcm.Containers;
using DspAdpcm.Formats;
using Xunit;

namespace DspAdpcm.Tests
{
    public static class BuildParseTests
    {
        public static void BuildParseCompareAudio(IAudioFormat audio, IAudioWriter writer, IAudioReader reader)
        {
            byte[] builtFile = writer.GetFile(audio);
            IAudioFormat parsedAudio = reader.ReadFormat(builtFile);
            Assert.Equal(audio, parsedAudio);
        }
    }
}
