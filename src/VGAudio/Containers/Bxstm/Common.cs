using VGAudio.Formats.GcAdpcm;

namespace VGAudio.Containers.Bxstm
{
    public static class Common
    {
        public static int SamplesToBytes(int sampleCount, BxstmCodec codec)
        {
            switch (codec)
            {
                case BxstmCodec.Adpcm:
                    return GcAdpcmHelpers.SampleCountToByteCount(sampleCount);
                case BxstmCodec.Pcm16Bit:
                    return sampleCount * 2;
                case BxstmCodec.Pcm8Bit:
                    return sampleCount;
                default:
                    return 0;
            }
        }

        public static int BytesToSamples(int byteCount, BxstmCodec codec)
        {
            switch (codec)
            {
                case BxstmCodec.Adpcm:
                    return GcAdpcmHelpers.NibbleCountToSampleCount(byteCount * 2);
                case BxstmCodec.Pcm16Bit:
                    return byteCount / 2;
                case BxstmCodec.Pcm8Bit:
                    return byteCount;
                default:
                    return 0;
            }
        }
    }
}
