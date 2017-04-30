using System.IO;
using VGAudio.Containers;
using VGAudio.Containers.Bxstm;
using VGAudio.Containers.Dsp;
using VGAudio.Containers.Wave;

namespace VGAudio.Cli
{
    internal static class CreateConfiguration
    {
        public static IConfiguration Wave(Options options)
        {
            var config = new WaveConfiguration();

            switch (options.OutFormat)
            {
                case AudioFormat.GcAdpcm:
                    throw new InvalidDataException("Can't use format GcAdpcm with Wave files");
                case AudioFormat.Pcm16:
                    config.Codec = WaveCodec.Pcm16Bit;
                    break;
                case AudioFormat.Pcm8:
                    config.Codec = WaveCodec.Pcm8Bit;
                    break;
            }

            return config;
        }

        public static IConfiguration Dsp(Options options)
        {
            var config = new DspConfiguration();

            switch (options.OutFormat)
            {
                case AudioFormat.Pcm16:
                    throw new InvalidDataException("Can't use format PCM16 with DSP files");
                case AudioFormat.Pcm8:
                    throw new InvalidDataException("Can't use format PCM8 with DSP files");
            }

            return config;
        }

        public static IConfiguration Idsp(Options options)
        {
            var config = new DspConfiguration();

            switch (options.OutFormat)
            {
                case AudioFormat.Pcm16:
                    throw new InvalidDataException("Can't use format PCM16 with IDSP files");
                case AudioFormat.Pcm8:
                    throw new InvalidDataException("Can't use format PCM8 with IDSP files");
            }

            return config;
        }

        public static IConfiguration Brstm(Options options)
        {
            var config = new BrstmConfiguration();

            switch (options.OutFormat)
            {
                case AudioFormat.GcAdpcm:
                    config.Codec = BxstmCodec.Adpcm;
                    break;
                case AudioFormat.Pcm16:
                    config.Codec = BxstmCodec.Pcm16Bit;
                    break;
                case AudioFormat.Pcm8:
                    throw new InvalidDataException("Can't use format PCM8 with BRSTM files");
            }

            return config;
        }

        public static IConfiguration Bcstm(Options options)
        {
            var config = new DspConfiguration();

            switch (options.OutFormat)
            {
                case AudioFormat.Pcm16:
                    throw new InvalidDataException("Can't use format PCM16 with BCSTM files");
                case AudioFormat.Pcm8:
                    throw new InvalidDataException("Can't use format PCM8 with BCSTM files");
            }

            return config;
        }

        public static IConfiguration Bfstm(Options options)
        {
            var config = new DspConfiguration();

            switch (options.OutFormat)
            {
                case AudioFormat.Pcm16:
                    throw new InvalidDataException("Can't use format PCM16 with BFSTM files");
                case AudioFormat.Pcm8:
                    throw new InvalidDataException("Can't use format PCM8 with BFSTM files");
            }

            return config;
        }
    }
}
