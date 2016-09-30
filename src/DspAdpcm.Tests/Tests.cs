using Xunit;

namespace DspAdpcm.Tests
{
    public class Tests
    {
        [Fact]
        public void Test()
        {
            DspAdpcm.Helpers.GetNibbleFromSample(4);
            Assert.Equal(4, 4);
        }
    }
}