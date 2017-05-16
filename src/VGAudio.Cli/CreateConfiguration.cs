using System.IO;
using VGAudio.Containers;
using VGAudio.Containers.Bxstm;
using VGAudio.Containers.Dsp;
using VGAudio.Containers.Idsp;
using VGAudio.Containers.Wave;

namespace VGAudio.Cli
{
    internal static class CreateConfiguration
    {
        public static Configuration Wave(Options options, Configuration inConfig = null)
        {
            var config = inConfig as WaveConfiguration ?? new WaveConfiguration();

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

        public static Configuration Dsp(Options options, Configuration inConfig = null)
        {
            var config = inConfig as DspConfiguration ?? new DspConfiguration();

            switch (options.OutFormat)
            {
                case AudioFormat.Pcm16:
                    throw new InvalidDataException("Can't use format PCM16 with DSP files");
                case AudioFormat.Pcm8:
                    throw new InvalidDataException("Can't use format PCM8 with DSP files");
            }

            return config;
        }

        public static Configuration Idsp(Options options, Configuration inConfig = null)
        {
            var config = inConfig as IdspConfiguration ?? new IdspConfiguration();

            switch (options.OutFormat)
            {
                case AudioFormat.Pcm16:
                    throw new InvalidDataException("Can't use format PCM16 with IDSP files");
                case AudioFormat.Pcm8:
                    throw new InvalidDataException("Can't use format PCM8 with IDSP files");
            }

            return config;
        }

        public static Configuration Brstm(Options options, Configuration inConfig = null)
        {
            var config = inConfig as BrstmConfiguration ?? new BrstmConfiguration();

            switch (options.OutFormat)
            {
                case AudioFormat.GcAdpcm:
                    config.Codec = BxstmCodec.Adpcm;
                    break;
                case AudioFormat.Pcm16:
                    config.Codec = BxstmCodec.Pcm16Bit;
                    break;
                case AudioFormat.Pcm8:
                    config.Codec = BxstmCodec.Pcm8Bit;
                    break;
            }

            return config;
        }

        public static Configuration Bcstm(Options options, Configuration inConfig = null)
        {
            var config = inConfig as BcstmConfiguration ?? new BcstmConfiguration();

            switch (options.OutFormat)
            {
                case AudioFormat.GcAdpcm:
                    config.Codec = BxstmCodec.Adpcm;
                    break;
                case AudioFormat.Pcm16:
                    config.Codec = BxstmCodec.Pcm16Bit;
                    break;
                case AudioFormat.Pcm8:
                    config.Codec = BxstmCodec.Pcm8Bit;
                    break;
            }

            return config;
        }

        public static Configuration Bfstm(Options options, Configuration inConfig = null)
        {
            var config = inConfig as BfstmConfiguration ?? new BfstmConfiguration();

            switch (options.OutFormat)
            {
                case AudioFormat.GcAdpcm:
                    config.Codec = BxstmCodec.Adpcm;
                    break;
                case AudioFormat.Pcm16:
                    config.Codec = BxstmCodec.Pcm16Bit;
                    break;
                case AudioFormat.Pcm8:
                    config.Codec = BxstmCodec.Pcm8Bit;
                    break;
            }

            return config;
        }
    }
}
