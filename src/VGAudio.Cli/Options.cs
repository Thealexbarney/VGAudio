using System.Collections.Generic;
using VGAudio.Codecs.CriAdx;
using VGAudio.Codecs.CriHca;
using VGAudio.Containers.Opus;
using VGAudio.Formats;
using VGAudio.Utilities;

namespace VGAudio.Cli
{
    internal class Options
    {
        public JobType Job { get; set; }
        public JobFiles Files { get; } = new JobFiles();

        public List<AudioFile> InFiles => Files.InFiles;
        public List<AudioFile> OutFiles => Files.OutFiles;

        public string InDir { get; set; }
        public string OutDir { get; set; }
        public bool Recurse { get; set; }
        public string OutTypeName { get; set; }

        public bool KeepConfiguration { get; set; }

        public bool Loop { get; set; }
        public bool NoLoop { get; set; }
        public int LoopStart { get; set; }
        public int LoopEnd { get; set; }
        public int LoopAlignment { get; set; }
        public int BlockSize { get; set; }
        public AudioFormat OutFormat { get; set; }
        public int Version { get; set; } // ADX
        public int FrameSize { get; set; } // ADX
        public int Filter { get; set; } = 2; // ADX
        public CriAdxType AdxType { get; set; } // ADX
        public string KeyString { get; set; } //ADX
        public ulong KeyCode { get; set; } //ADX
        public Endianness? Endianness { get; set; }

        public CriHcaQuality HcaQuality { get; set; }
        public int Bitrate { get; set; }
        public bool LimitBitrate { get; set; }

        public NxOpusHeaderType NxOpusHeaderType { get; set; } // Switch Opus
        public bool EncodeCbr { get; set; }
    }

    internal class JobFiles
    {
        public List<AudioFile> InFiles { get; } = new List<AudioFile>();
        public List<AudioFile> OutFiles { get; } = new List<AudioFile>();
    }

    internal class AudioFile
    {
        public AudioFile() { }
        public AudioFile(string path)
        {
            Path = path;
            Type = CliArguments.GetFileTypeFromName(path);
        }

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
        Brwav,
        Bcwav,
        Bfwav,
        Bcstp,
        Bfstp,
        Hps,
        Adx,
        Hca,
        Genh,
        Atrac9,
        NxOpus,
        OggOpus,
        McAdpcm,
        Fuz
    }
}
