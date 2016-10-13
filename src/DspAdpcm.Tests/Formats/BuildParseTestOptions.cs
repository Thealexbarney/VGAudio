namespace DspAdpcm.Tests.Formats
{
    internal static class BuildParseTestOptions
    {
        public static readonly double StreamLengthSeconds = 1;
        public static readonly int Frequency = 48000;
        public static int Samples => (int)(Frequency * StreamLengthSeconds);
    }
}
