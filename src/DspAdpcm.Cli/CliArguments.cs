namespace DspAdpcm.Cli
{
    internal class CliArguments
    {
        public static Options Parse(string[] arguments)
        {
            return new Options
            {
                InFilePath = @"a.wav",
                InFileType = FileType.Wave,
                OutFilePath = @"b.brstm",
                OutFileType = FileType.Brstm,
                KeepConfiguration = true
            };
        }
    }
}
