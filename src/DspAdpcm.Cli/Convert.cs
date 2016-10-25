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
        private Convert() {}
        private PcmStream Pcm { get; set; }
        private AdpcmStream Adpcm { get; set; }
        private object Configuration { get; set; }

        public static bool ConvertFile(Options options)
        {
            if (options.Job != JobType.Convert) return false;

            var converter = new Convert();
            converter.ReadFile(options.InFilePath, options.InFileType);

            if (!(options.InFileType == options.OutFileType && options.KeepConfiguration))
            {
                converter.Configuration = null;
            }

            converter.EncodeFile(options);
            converter.WriteFile(options.OutFilePath, options.OutFileType);

            return true;
        }

        private void ReadFile(string fileName, FileType fileType)
        {
            using (var stream = new FileStream(fileName, FileMode.Open))
            {
                switch (fileType)
                {
                    case FileType.Wave:
                        Pcm = new Wave(stream).AudioStream;
                        break;
                    case FileType.Dsp:
                        var dsp = new Dsp(stream);
                        Adpcm = dsp.AudioStream;
                        Configuration = dsp.Configuration;
                        break;
                    case FileType.Idsp:
                        var idsp = new Idsp(stream);
                        Adpcm = idsp.AudioStream;
                        Configuration = idsp.Configuration;
                        break;
                    case FileType.Brstm:
                        var brstm = new Brstm(stream);
                        Adpcm = brstm.AudioStream;
                        Configuration = brstm.Configuration;
                        break;
                    case FileType.Bcstm:
                        var bcstm = new Bcstm(stream);
                        Adpcm = bcstm.AudioStream;
                        Configuration = bcstm.Configuration;
                        break;
                    case FileType.Bfstm:
                        var bfstm = new Bfstm(stream);
                        Adpcm = bfstm.AudioStream;
                        Configuration = bfstm.Configuration;
                        break;
                    case FileType.Genh:
                        Adpcm = new Genh(stream).AudioStream;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(fileType), fileType, null);
                }
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
                        new Dsp(Adpcm, (DspConfiguration)Configuration).WriteFile(stream);
                        break;
                    case FileType.Idsp:
                        new Idsp(Adpcm, (IdspConfiguration)Configuration).WriteFile(stream);
                        break;
                    case FileType.Brstm:
                        new Brstm(Adpcm, (BrstmConfiguration)Configuration).WriteFile(stream);
                        break;
                    case FileType.Bcstm:
                        new Bcstm(Adpcm, (BcstmConfiguration)Configuration).WriteFile(stream);
                        break;
                    case FileType.Bfstm:
                        new Bfstm(Adpcm, (BfstmConfiguration)Configuration).WriteFile(stream);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(fileType), fileType, null);
                }
            }
        }

        private void EncodeFile(Options options)
        {
            AudioType inType = AudioTypes[options.InFileType];
            AudioType outType = AudioTypes[options.OutFileType];

            if (inType == AudioType.Pcm && outType == AudioType.Adpcm)
            {
                Adpcm = Encode.PcmToAdpcm(Pcm);
            }

            if (inType == AudioType.Adpcm && outType == AudioType.Pcm)
            {
                Pcm = Decode.AdpcmtoPcm(Adpcm);
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
