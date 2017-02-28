using VGAudio.Containers;
using VGAudio.Formats;
using Xunit;

namespace VGAudio.Tests
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
