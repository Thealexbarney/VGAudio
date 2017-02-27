namespace DspAdpcm.Cli
{
    public static class DspAdpcmCli
    {
        public static int Main(string[] args)
        {
            return Converter.RunConverterCli(args) ? 0 : 1;
        }
    }
}
