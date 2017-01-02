using System;
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
        private LoopingPcmStream Pcm { get; set; }
        private AdpcmStream Adpcm { get; set; }
        private object Configuration { get; set; }

        public static bool ConvertFile(Options options)
        {
            if (options.Job != JobType.Convert) return false;

            var converter = new Convert();

            foreach (AudioFile file in options.InFiles)
            {
                converter.ReadFile(file);
            }

            if (!options.KeepConfiguration)
            {
                converter.Configuration = null;
            }

            converter.EncodeFiles(options);
            converter.WriteFile(options.OutFiles[0].Path, options.OutFiles[0].Type);

            return true;
        }

        private void ReadFile(AudioFile file)
        {
            using (var stream = new FileStream(file.Path, FileMode.Open))
            {
                switch (file.Type)
                {
                    case FileType.Wave:
                        file.Pcm = new Wave(stream).AudioStream;
                        break;
                    case FileType.Dsp:
                        var dsp = new Dsp(stream);
                        file.Adpcm = dsp.AudioStream;
                        Configuration = dsp.Configuration;
                        break;
                    case FileType.Idsp:
                        var idsp = new Idsp(stream);
                        file.Adpcm = idsp.AudioStream;
                        Configuration = idsp.Configuration;
                        break;
                    case FileType.Brstm:
                        var brstm = new Brstm(stream);
                        file.Adpcm = brstm.AudioStream as AdpcmStream;
                        file.Pcm = brstm.AudioStream as PcmStream;
                        Configuration = brstm.Configuration;
                        break;
                    case FileType.Bcstm:
                        var bcstm = new Bcstm(stream);
                        file.Adpcm = bcstm.AudioStream;
                        Configuration = bcstm.Configuration;
                        break;
                    case FileType.Bfstm:
                        var bfstm = new Bfstm(stream);
                        file.Adpcm = bfstm.AudioStream;
                        Configuration = bfstm.Configuration;
                        break;
                    case FileType.Genh:
                        file.Adpcm = new Genh(stream).AudioStream;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(file.Type), file.Type, null);
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
                        new Dsp(Adpcm, Configuration as DspConfiguration).WriteFile(stream);
                        break;
                    case FileType.Idsp:
                        new Idsp(Adpcm, Configuration as IdspConfiguration).WriteFile(stream);
                        break;
                    case FileType.Brstm:
                        var preferredStream = (Adpcm as LoopingTrackStream) ?? (Pcm as LoopingTrackStream);
                        new Brstm(preferredStream, Configuration as BrstmConfiguration).WriteFile(stream);
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

        private void EncodeFiles(Options options)
        {
            AudioCodec outCodec = options.OutFiles[0].Codec;

            foreach (AudioFile file in options.InFiles)
            {
                if (outCodec == AudioCodec.Adpcm)
                {
                    file.ConvertToAdpcm();
                    Adpcm = Adpcm ?? new AdpcmStream(file.Adpcm.NumSamples, file.Adpcm.SampleRate);
                    Adpcm.Add(file.Channels == null ? file.Adpcm : file.Adpcm.GetChannels(file.Channels));
                }
                else if (outCodec == AudioCodec.Pcm)
                {
                    file.ConvertToPcm();
                    Pcm = Pcm ?? new LoopingPcmStream(file.Pcm.NumSamples, file.Pcm.SampleRate);
                    Pcm.Add(file.Channels == null ? file.Pcm : file.Pcm.GetChannels(file.Channels));
                }
            }

            var outStream = (outCodec == AudioCodec.Adpcm)
                ? Adpcm as LoopingTrackStream
                : Pcm as LoopingTrackStream;

            if (options.NoLoop)
            {
                outStream.SetLoop(false);
            }

            if (options.Loop)
            {
                outStream.SetLoop(options.LoopStart, options.LoopEnd);
            }
        }
    }
}
