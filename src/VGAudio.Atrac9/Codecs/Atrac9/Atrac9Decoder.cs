using System;
using VGAudio.Utilities;

namespace VGAudio.Codecs.Atrac9
{
    public class Atrac9Decoder
    {
        public Atrac9Config Config { get; private set; }

        private Frame Frame { get; set; }
        private BitReader Reader { get; set; }
        private bool _initialized;

        public void Initialize(byte[] configData)
        {
            Config = new Atrac9Config(configData);
            Frame = new Frame(Config);
            Reader = new BitReader(null);
            _initialized = true;
        }

        public void Decode(byte[] atrac9Data, short[][] pcmOut)
        {
            if (!_initialized) throw new InvalidOperationException("Decoder must be initialized before decoding.");

            ValidateBufferLength(pcmOut);
            Reader.SetBuffer(atrac9Data);
            DecodeSuperFrame(pcmOut);
        }

        private void ValidateBufferLength(short[][] buffer)
        {
            if (buffer == null || buffer.Length < Config.ChannelCount)
            {
                throw new ArgumentException("PCM buffer is too small");
            }

            for (int i = 0; i < Config.ChannelCount; i++)
            {
                if (buffer[i]?.Length < Config.SuperframeSamples)
                {
                    throw new ArgumentException("PCM buffer is too small");
                }
            }
        }

        private void DecodeSuperFrame(short[][] pcmOut)
        {
            for (int i = 0; i < Config.FramesPerSuperframe; i++)
            {
                Frame.FrameIndex = i;
                DecodeFrame(Reader, Frame);
                PcmFloatToShort(pcmOut, i * Config.FrameSamples);
                Reader.AlignPosition(8);
            }
        }

        private void PcmFloatToShort(short[][] pcmOut, int start)
        {
            int endSample = start + Config.FrameSamples;
            int channelNum = 0;
            foreach (Block block in Frame.Blocks)
            {
                foreach (Channel channel in block.Channels)
                {
                    double[] pcmSrc = channel.Pcm;
                    short[] pcmDest = pcmOut[channelNum++];
                    for (int d = 0, s = start; s < endSample; d++, s++)
                    {
                        double sample = pcmSrc[d];
                        // Not using Math.Round because it's ~20x slower on 64-bit
                        int roundedSample = (int)Math.Floor(sample + 0.5);
                        pcmDest[s] = Helpers.Clamp16(roundedSample);
                    }
                }
            }
        }

        private static void DecodeFrame(BitReader reader, Frame frame)
        {
            Unpack.UnpackFrame(reader, frame);

            foreach (Block block in frame.Blocks)
            {
                Quantization.DequantizeSpectra(block);
                Stereo.ApplyIntensityStereo(block);
                Quantization.ScaleSpectrum(block);
                BandExtension.ApplyBandExtension(block);
                ImdctBlock(block);
            }
        }

        private static void ImdctBlock(Block block)
        {
            foreach (Channel channel in block.Channels)
            {
                channel.Mdct.RunImdct(channel.Spectra, channel.Pcm);
            }
        }
    }
}
