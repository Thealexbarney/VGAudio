using System.IO;
using VGAudio.Codecs.CriAdx;
using VGAudio.Codecs.CriHca;
using VGAudio.Containers;
using VGAudio.Containers.Adx;
using VGAudio.Containers.Dsp;
using VGAudio.Containers.Hca;
using VGAudio.Containers.Hps;
using VGAudio.Containers.Idsp;
using VGAudio.Containers.NintendoWare;
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

            if (options.LoopAlignment > 0)
            {
                config.LoopPointAlignment = options.LoopAlignment;
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

            if (options.BlockSize > 0)
            {
                config.BlockSize = options.BlockSize;
            }

            return config;
        }

        public static Configuration Bxstm(Options options, Configuration inConfig = null)
        {
            var config = inConfig as BxstmConfiguration ?? new BxstmConfiguration();

            switch (options.OutFormat)
            {
                case AudioFormat.GcAdpcm:
                    config.Codec = NwCodec.GcAdpcm;
                    break;
                case AudioFormat.Pcm16:
                    config.Codec = NwCodec.Pcm16Bit;
                    break;
                case AudioFormat.Pcm8:
                    config.Codec = NwCodec.Pcm8Bit;
                    break;
            }

            if (options.LoopAlignment > 0)
            {
                config.LoopPointAlignment = options.LoopAlignment;
            }

            return config;
        }

        public static Configuration Hps(Options options, Configuration inConfig = null)
        {
            var config = inConfig as HpsConfiguration ?? new HpsConfiguration();

            switch (options.OutFormat)
            {
                case AudioFormat.Pcm16:
                    throw new InvalidDataException("Can't use format PCM16 with HPS files");
                case AudioFormat.Pcm8:
                    throw new InvalidDataException("Can't use format PCM8 with HPS files");
            }

            return config;
        }

        public static Configuration Adx(Options options, Configuration inConfig = null)
        {
            var config = inConfig as AdxConfiguration ?? new AdxConfiguration();

            if (options.Version != 0) config.Version = options.Version;
            if (options.FrameSize != 0) config.FrameSize = options.FrameSize;
            if (options.Filter >= 0 && options.Filter <= 3) config.Filter = options.Filter;
            if (options.AdxType != default(CriAdxType)) config.Type = options.AdxType;
            if (options.KeyString != null)
            {
                config.EncryptionKey = new CriAdxKey(options.KeyString);
                config.EncryptionType = 8;
            }
            if (options.KeyCode != 0)
            {
                config.EncryptionKey = new CriAdxKey(options.KeyCode);
                config.EncryptionType = 9;
            }
            return config;
        }

        public static Configuration Hca(Options options, Configuration inConfig = null)
        {
            HcaConfiguration config = inConfig as HcaConfiguration ?? new HcaConfiguration();

            if (options.HcaQuality != CriHcaQuality.NotSet) config.Quality = options.HcaQuality;
            config.LimitBitrate = options.LimitBitrate;
            if (options.Bitrate != 0) config.Bitrate = options.Bitrate;

            return config;
        }
    }
}
