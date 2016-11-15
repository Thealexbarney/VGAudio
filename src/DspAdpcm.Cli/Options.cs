using System.Collections.Generic;

namespace DspAdpcm.Cli
{
    internal class Options
    {
        public JobType Job { get; set; }

        public List<AudioFile> InFiles { get; } = new List<AudioFile>();
        public List<AudioFile> OutFiles { get; } = new List<AudioFile>();

        public bool KeepConfiguration { get; set; }

        public bool Loop { get; set; }
        public bool NoLoop { get; set; }
        public int LoopStart { get; set; }
        public int LoopEnd { get; set; }
    }

    internal class AudioFile
    {
        public string Path { get; set; }
        public FileType Type { get; set; }
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
