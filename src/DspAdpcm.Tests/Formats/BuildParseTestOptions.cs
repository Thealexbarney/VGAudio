namespace DspAdpcm.Tests.Formats
{
    internal static class BuildParseTestOptions
    {
        public static readonly double StreamLengthSeconds = 1;
        public static readonly int SampleRate = 48000;
        public static int Samples => (int)(SampleRate * StreamLengthSeconds);
    }
}
