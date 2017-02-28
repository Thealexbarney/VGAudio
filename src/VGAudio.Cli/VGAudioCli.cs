namespace VGAudio.Cli
{
    public static class VGAudioCli
    {
        public static int Main(string[] args)
        {
            return Converter.RunConverterCli(args) ? 0 : 1;
        }
    }
}
