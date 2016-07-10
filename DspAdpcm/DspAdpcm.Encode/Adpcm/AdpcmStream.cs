using System;
using System.Collections.Generic;
using System.Linq;

namespace DspAdpcm.Encode.Adpcm
{
    public class AdpcmStream
    {
        public const int BytesPerBlock = 8;
        public const int SamplesPerBlock = 14;
        public const int NibblesPerBlock = 16;

        public int LoopStart { get; set; }
        public int LoopEnd { get; set; }
        private IAudioStream InputAudioStream { get; set; }

        public int NumSamples { get; }
        private int NumNibbles => GetNibbleFromSample(NumSamples);
        private int SampleRate { get; }
        public bool LoopFlag { get; }
        private short Format { get; } = 0; /* 0 for ADPCM */

        private int StartAddr => GetNibbleAddress(LoopFlag ? LoopStart : 0);
        private int EndAddr => GetNibbleAddress(LoopFlag ? LoopEnd : NumSamples - 1);
        private static int CurAddr => GetNibbleAddress(0);

        public short[] Coefs { get; set; }
        public short Gain { get; set; }

        private short PredScale { get; set; }
        private short Hist1 { get; } = 0;
        private short Hist2 { get; } = 0;

        public short LoopPredScale { get; set; }
        public short LoopHist1 { get; set; }
        public short LoopHist2 { get; set; }
        private static IEnumerable<short> Pad => new short[11];

        public byte[] AudioData { get; set; }

        public int DspFileSize => 0x60 + GetBytesForAdpcmSamples(NumSamples);

        public AdpcmStream(int samples, int sampleRate)
        {
            NumSamples = samples;
            SampleRate = sampleRate;
            AudioData = new byte[GetBytesForAdpcmSamples(samples)];
        }

        public AdpcmStream(int samples, int sampleRate, int loopStart, int loopEnd)
            : this(samples, sampleRate)
        {
            LoopFlag = true;
            LoopStart = loopStart;
            LoopEnd = loopEnd;
        }

        public AdpcmStream(IAudioStream stream)
            : this(stream.GetNumSamples(), stream.GetSampleRate())
        {
            InputAudioStream = stream;
        }

        public AdpcmStream(IAudioStream stream, int loopStart, int loopEnd)
            : this(stream.GetNumSamples(), stream.GetSampleRate(), loopStart, loopEnd)
        {
            InputAudioStream = stream;
        }

        private static int GetNibbleFromSample(int samples)
        {
            int blocks = samples / SamplesPerBlock;
            int extraSamples = samples % SamplesPerBlock;
            int extraNibbles = extraSamples == 0 ? 0 : extraSamples + 2;

            return NibblesPerBlock * blocks + extraNibbles;
        }

        private static int GetNibbleAddress(int sample)
        {
            int blocks = sample / SamplesPerBlock;
            int extraSamples = sample % SamplesPerBlock;

            return NibblesPerBlock * blocks + extraSamples + 2;
        }

        private static int GetBytesForAdpcmSamples(int samples)
        {
            int extraBytes = 0;
            int blocks = samples / SamplesPerBlock;
            int extraSamples = samples % SamplesPerBlock;

            if (extraSamples != 0)
            {
                extraBytes = (extraSamples / 2) + (extraSamples % 2) + 1;
            }

            return BytesPerBlock * blocks + extraBytes;
        }

        public void Encode()
        {
            Coefs = Adpcm.Encode.DspCorrelateCoefs(InputAudioStream.GetAudioData(), NumSamples);

            /* Execute encoding-predictor for each block */
            short[] convSamps = new short[2 + SamplesPerBlock];
            byte[] block = new byte[BytesPerBlock];

            int blockCount = 0;
            foreach (var inBlock in InputAudioStream.GetAudioData().Batch(SamplesPerBlock))
            {
                Array.Copy(inBlock, 0, convSamps, 2, SamplesPerBlock);

                Adpcm.Encode.DspEncodeFrame(convSamps, SamplesPerBlock, block, Coefs);

                convSamps[0] = convSamps[14];
                convSamps[1] = convSamps[15];

                int numSamples = Math.Min(NumSamples - blockCount * SamplesPerBlock, SamplesPerBlock);
                Array.Copy(block, 0, AudioData, blockCount++ * BytesPerBlock, GetBytesForAdpcmSamples(numSamples));
            }
            if (LoopFlag)
            {
                Adpcm.Encode.GetLoopContext(this);
            }
        }

        public IEnumerable<byte> GetHeader()
        {
            PredScale = AudioData[0];

            var output = new List<byte>();
            output.AddRange(BitConverter.GetBytes(NumSamples).Reverse());
            output.AddRange(BitConverter.GetBytes(NumNibbles).Reverse());
            output.AddRange(BitConverter.GetBytes(SampleRate).Reverse());
            output.AddRange(BitConverter.GetBytes((short)(LoopFlag ? 1 : 0)).Reverse());
            output.AddRange(BitConverter.GetBytes(Format).Reverse());
            output.AddRange(BitConverter.GetBytes(StartAddr).Reverse());
            output.AddRange(BitConverter.GetBytes(EndAddr).Reverse());
            output.AddRange(BitConverter.GetBytes(CurAddr).Reverse());
            output.AddRange(Coefs.SelectMany(x => BitConverter.GetBytes(x).Reverse()));
            output.AddRange(BitConverter.GetBytes(Gain).Reverse());
            output.AddRange(BitConverter.GetBytes(PredScale).Reverse());
            output.AddRange(BitConverter.GetBytes(Hist1).Reverse());
            output.AddRange(BitConverter.GetBytes(Hist2).Reverse());
            output.AddRange(BitConverter.GetBytes(LoopPredScale).Reverse());
            output.AddRange(BitConverter.GetBytes(LoopHist1).Reverse());
            output.AddRange(BitConverter.GetBytes(LoopHist2).Reverse());
            output.AddRange(Pad.SelectMany(x => BitConverter.GetBytes(x).Reverse()));

            return output.ToArray();
        }

        public IEnumerable<byte> GetDspFile()
        {
            return GetHeader().Concat(AudioData);
        }
    }
}
