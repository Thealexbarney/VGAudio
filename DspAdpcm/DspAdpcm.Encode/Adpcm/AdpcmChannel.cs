using System;
using static DspAdpcm.Encode.Adpcm.Helpers;

namespace DspAdpcm.Encode.Adpcm
{
    public class AdpcmChannel
    {
        public byte[] AudioData { get; set; }
        public IPcmChannel AudioSource { get; set; }

        public int NumSamples => AudioData?.Length ?? AudioSource?.GetNumSamples() ?? 0;

        public short[] Coefs { get; set; }
        public short Hist1 { get; } = 0;
        public short Hist2 { get; } = 0;

        public short LoopPredScale { get; set; }
        public short LoopHist1 { get; set; }
        public short LoopHist2 { get; set; }

        public AdpcmChannel(IPcmChannel channel)
        {
            AudioSource = channel;
            AudioData = new byte[GetBytesForAdpcmSamples(channel.GetNumSamples())];
        }

        public void Encode()
        {
            if (AudioSource == null) return;

            Coefs = Adpcm.Encode.DspCorrelateCoefs(AudioSource.GetAudioChannelData(), AudioSource.GetNumSamples());

            /* Execute encoding-predictor for each block */
            short[] convSamps = new short[2 + SamplesPerBlock];
            byte[] block = new byte[BytesPerBlock];

            int blockCount = 0;
            foreach (var inBlock in AudioSource.GetAudioChannelData().Batch(SamplesPerBlock))
            {
                Array.Copy(inBlock, 0, convSamps, 2, SamplesPerBlock);

                Adpcm.Encode.DspEncodeFrame(convSamps, SamplesPerBlock, block, Coefs);

                convSamps[0] = convSamps[14];
                convSamps[1] = convSamps[15];

                int numSamples = Math.Min(AudioSource.GetNumSamples() - blockCount * SamplesPerBlock, SamplesPerBlock);
                Array.Copy(block, 0, AudioData, blockCount++ * BytesPerBlock, GetBytesForAdpcmSamples(numSamples));
            }
            //if (Looping)
            //{
            //    Adpcm.Encode.GetLoopContext(this);
            //}
        }
    }
}
