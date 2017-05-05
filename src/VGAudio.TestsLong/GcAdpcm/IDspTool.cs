using VGAudio.Formats.GcAdpcm;

namespace VGAudio.TestsLong.GcAdpcm
{
    public interface IDspTool
    {
        GcAdpcmChannel EncodeChannel(short[] pcm);
        short[] DspCorrelateCoefs(short[] pcm);
        void DspEncodeFrame(short[] pcmInOut, int sampleCount, byte[] adpcmOut, short[] coefsIn);
        short[] DecodeChannel(GcAdpcmChannel channel);
        short[] DecodeAdpcm(byte[] adpcm, short[] coefs, int sampleCount);
    }
}