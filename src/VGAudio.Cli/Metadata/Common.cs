using System;
using VGAudio.Containers.Bxstm;

namespace VGAudio.Cli.Metadata
{
    internal class Common
    {
        public int SampleCount { get; set; }
        public int SampleRate { get; set; }
        public int ChannelCount { get; set; }
        public bool Looping { get; set; }
        public int LoopStart { get; set; }
        public int LoopEnd { get; set; }
        public AudioFormat Format { get; set; }

        public static AudioFormat FromBxstm(BxstmCodec codec)
        {
            switch (codec)
            {
                case BxstmCodec.Pcm8Bit:
                    return AudioFormat.Pcm8;
                case BxstmCodec.Pcm16Bit:
                    return AudioFormat.Pcm16;
                case BxstmCodec.Adpcm:
                    return AudioFormat.GcAdpcm;
                default:
                    throw new ArgumentOutOfRangeException(nameof(codec), codec, null);
            }
        }
    }
}
