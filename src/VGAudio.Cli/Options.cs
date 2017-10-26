using System.Collections.Generic;
using VGAudio.Codecs.CriAdx;
using VGAudio.Codecs.CriHca;
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
        public int LoopAlignment { get; set; }
        public int BlockSize { get; set; }
        public AudioFormat OutFormat { get; set; }
        public int Version { get; set; } // ADX
        public int FrameSize { get; set; } // ADX
        public int Filter { get; set; } = 2; // ADX
        public CriAdxType AdxType { get; set; } // ADX
        public string KeyString { get; set; } //ADX
        public ulong KeyCode { get; set; } //ADX

        public CriHcaQuality HcaQuality { get; set; }
        public int Bitrate { get; set; }
        public bool LimitBitrate { get; set; }
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
        Brwav,
        Bcwav,
        Bfwav,
        Hps,
        Adx,
        Hca,
        Genh
    }
}
