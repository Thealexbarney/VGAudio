using VGAudio.Codecs.GcAdpcm;
using VGAudio.Formats.GcAdpcm;

namespace VGAudio.Tools.GcAdpcm
{
    public class DspToolVGAudio : IDspTool
    {
        public GcAdpcmChannel EncodeChannel(short[] pcm)
        {
            int sampleCount = pcm.Length;
            short[] coefs = GcAdpcmCoefficients.CalculateCoefficients(pcm);
            byte[] adpcm = GcAdpcmEncoder.Encode(pcm, coefs);

            return new GcAdpcmChannel(adpcm, coefs, sampleCount);
        }

        public short[] DspCorrelateCoefs(short[] pcm)
        {
            return GcAdpcmCoefficients.CalculateCoefficients(pcm);
        }

        public void DspEncodeFrame(short[] pcmInOut, int sampleCount, byte[] adpcmOut, short[] coefsIn)
        {
            GcAdpcmEncoder.DspEncodeFrame(pcmInOut, sampleCount, adpcmOut, coefsIn);
        }

        public short[] DecodeChannel(GcAdpcmChannel channel)
        {
            return DecodeAdpcm(channel.GetAdpcmAudio(), channel.Coefs, channel.SampleCount);
        }

        public short[] DecodeAdpcm(byte[] adpcm, short[] coefs, int sampleCount)
        {
            return GcAdpcmDecoder.Decode(adpcm, coefs, new GcAdpcmParameters { SampleCount = sampleCount });
        }
    }
}
