﻿namespace DspAdpcm.Cli
{
    internal class Options
    {
        public JobType Job { get; set; }

        public FileType InFileType { get; set; }
        public string InFilePath { get; set; }

        public FileType OutFileType { get; set; }
        public string OutFilePath { get; set; }

        public bool KeepConfiguration { get; set; }

        public bool Loop { get; set; }
        public bool NoLoop { get; set; }
        public int LoopStart { get; set; }
        public int LoopEnd { get; set; }
    }

    internal enum JobType
    {
        Convert,
        Batch,
        Metadata
    }

    internal enum FileType
    {
        NotSet = 0,
        Wave,
        Dsp,
        Idsp,
        Brstm,
        Bcstm,
        Bfstm,
        Genh
    }

    internal enum AudioType
    {
        Pcm,
        Adpcm
    }
}