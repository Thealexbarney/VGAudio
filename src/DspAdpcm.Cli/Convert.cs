using System;
using System.Collections.Generic;
using System.IO;
using DspAdpcm.Adpcm;
using DspAdpcm.Adpcm.Formats;
using DspAdpcm.Adpcm.Formats.Configuration;
using DspAdpcm.Pcm;
using DspAdpcm.Pcm.Formats;

namespace DspAdpcm.Cli
{
    internal class Convert
    {
        private Convert() { }
        private PcmStream Pcm { get; set; }
        private AdpcmStream Adpcm { get; set; }
        private object Configuration { get; set; }

        public static bool ConvertFile(Options options)
        {
            if (options.Job != JobType.Convert) return false;

            var converter = new Convert();

            foreach (AudioFile file in options.InFiles)
            {
                converter.ReadFile(file.Path, file.Type);
            }

            if (!options.KeepConfiguration)
            {
                converter.Configuration = null;
            }

            converter.EncodeFile(options);
            converter.WriteFile(options.OutFiles[0].Path, options.OutFiles[0].Type);

            return true;
        }

        private void ReadFile(string fileName, FileType fileType)
        {
            AdpcmStream adpcm = null;
            using (var stream = new FileStream(fileName, FileMode.Open))
            {
                switch (fileType)
                {
                    case FileType.Wave:
                        Pcm = new Wave(stream).AudioStream;
                        break;
                    case FileType.Dsp:
                        var dsp = new Dsp(stream);
                        adpcm = dsp.AudioStream;
                        Configuration = dsp.Configuration;
                        break;
                    case FileType.Idsp:
                        var idsp = new Idsp(stream);
                        adpcm = idsp.AudioStream;
                        Configuration = idsp.Configuration;
                        break;
                    case FileType.Brstm:
                        var brstm = new Brstm(stream);
                        adpcm = brstm.AudioStream;
                        Configuration = brstm.Configuration;
                        break;
                    case FileType.Bcstm:
                        var bcstm = new Bcstm(stream);
                        adpcm = bcstm.AudioStream;
                        Configuration = bcstm.Configuration;
                        break;
                    case FileType.Bfstm:
                        var bfstm = new Bfstm(stream);
                        adpcm = bfstm.AudioStream;
                        Configuration = bfstm.Configuration;
                        break;
                    case FileType.Genh:
                        adpcm = new Genh(stream).AudioStream;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(fileType), fileType, null);
                }
            }
            if (adpcm == null) return;

            if (Adpcm == null)
            {
                Adpcm = adpcm;
            }
            else
            {
                Adpcm.Add(adpcm);
            }
        }

        private void WriteFile(string fileName, FileType fileType)
        {
            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                switch (fileType)
                {
                    case FileType.Wave:
                        new Wave(Pcm).WriteFile(stream);
                        break;
                    case FileType.Dsp:
                        new Dsp(Adpcm, Configuration as DspConfiguration).WriteFile(stream);
                        break;
                    case FileType.Idsp:
                        new Idsp(Adpcm, Configuration as IdspConfiguration).WriteFile(stream);
                        break;
                    case FileType.Brstm:
                        new Brstm(Adpcm, Configuration as BrstmConfiguration).WriteFile(stream);
                        break;
                    case FileType.Bcstm:
                        new Bcstm(Adpcm, Configuration as BcstmConfiguration).WriteFile(stream);
                        break;
                    case FileType.Bfstm:
                        new Bfstm(Adpcm, Configuration as BfstmConfiguration).WriteFile(stream);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(fileType), fileType, null);
                }
            }
        }

        private void EncodeFile(Options options)
        {
            AudioType inType = AudioTypes[options.InFiles[0].Type];
            AudioType outType = AudioTypes[options.OutFiles[0].Type];

            if (inType == AudioType.Pcm && outType == AudioType.Adpcm)
            {
#if NOPARALLEL
                Adpcm = Encode.PcmToAdpcm(Pcm);
#else
                Adpcm = Encode.PcmToAdpcmParallel(Pcm);
#endif
            }

            if (inType == AudioType.Adpcm && outType == AudioType.Pcm)
            {
#if NOPARALLEL
                Pcm = Decode.AdpcmtoPcm(Adpcm);
#else
                Pcm = Decode.AdpcmtoPcmParallel(Adpcm);
#endif
            }

            if (options.NoLoop && outType == AudioType.Adpcm)
            {
                Adpcm.SetLoop(false);
            }

            if (options.Loop && outType == AudioType.Adpcm)
            {
                Adpcm.SetLoop(options.LoopStart, options.LoopEnd);
            }
        }

        private static readonly Dictionary<FileType, AudioType> AudioTypes =
            new Dictionary<FileType, AudioType>
            {
                [FileType.Wave] = AudioType.Pcm,
                [FileType.Dsp] = AudioType.Adpcm,
                [FileType.Idsp] = AudioType.Adpcm,
                [FileType.Brstm] = AudioType.Adpcm,
                [FileType.Bcstm] = AudioType.Adpcm,
                [FileType.Bfstm] = AudioType.Adpcm,
                [FileType.Genh] = AudioType.Adpcm
            };
    }
}
