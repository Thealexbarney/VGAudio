using System;
using System.Collections.Generic;
using DspAdpcm.Adpcm;
using DspAdpcm.Pcm;

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
        public AudioCodec Codec => AudioTypes[Type];
        public PcmStream Pcm { get; set; }
        public AdpcmStream Adpcm { get; set; }

        public void ConvertToPcm()
        {
            if (Pcm != null) return;

            if (Adpcm == null)
            {
                throw new InvalidOperationException("Adpcm is null");
            }

#if NOPARALLEL
            Pcm = Decode.AdpcmtoPcm(Adpcm);
#else
            Pcm = Decode.AdpcmtoPcmParallel(Adpcm);
#endif
        }
        public void ConvertToAdpcm()
        {
            if (Adpcm != null) return;

            if (Pcm == null)
            {
                throw new InvalidOperationException("Pcm is null");
            }

#if NOPARALLEL
            Adpcm = Encode.PcmToAdpcm(Pcm);
#else
            Adpcm = Encode.PcmToAdpcmParallel(Pcm);
#endif
        }

        private static readonly Dictionary<FileType, AudioCodec> AudioTypes =
            new Dictionary<FileType, AudioCodec>
            {
                [FileType.NotSet] = AudioCodec.NotSet,
                [FileType.Wave] = AudioCodec.Pcm,
                [FileType.Dsp] = AudioCodec.Adpcm,
                [FileType.Idsp] = AudioCodec.Adpcm,
                [FileType.Brstm] = AudioCodec.Adpcm,
                [FileType.Bcstm] = AudioCodec.Adpcm,
                [FileType.Bfstm] = AudioCodec.Adpcm,
                [FileType.Genh] = AudioCodec.Adpcm
            };
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

    internal enum AudioCodec
    {
        NotSet = 0,
        Pcm,
        Adpcm
    }
}
