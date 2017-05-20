using System.Collections.Generic;
using VGAudio.Formats;

namespace VGAudio.Cli
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
        public AudioFormat OutFormat { get; set; }
    }

    internal class AudioFile
    {
        public string Path { get; set; }
        public FileType Type { get; set; }
        public AudioData Audio { get; set; }
        public List<int> Channels { get; set; }
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
        Hps,
        Genh
    }
}
