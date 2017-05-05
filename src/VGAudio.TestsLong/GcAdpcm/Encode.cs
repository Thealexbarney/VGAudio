using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VGAudio.Containers;
using VGAudio.Formats;
using VGAudio.Formats.GcAdpcm;

namespace VGAudio.TestsLong.GcAdpcm
{
    public class Encode
    {
        private IDspTool DllA { get; }
        private IDspTool DllB { get; }

        public Encode(IDspTool dllA, IDspTool dllB)
        {
            DllA = dllA;
            DllB = dllB;
        }

        public IEnumerable<Result> CompareWave(string path)
        {
            byte[] wave = File.ReadAllBytes(path);
            var pcm = new WaveReader().ReadFormat(wave) as Pcm16Format;
            if (pcm == null) return new Result[0];

            return ComparePcm(pcm, path);
        }

        public IEnumerable<Result> ComparePcm(Pcm16Format pcm, string path)
        {
            return pcm.Channels.AsParallel().Select((x, i) =>
            {
                Result result = CompareEncodingCoarse(x) ? new Result { Equal = true } : CompareEncodingFine(x, DllA, DllB);
                result.Channel = i;
                result.Filename = path;
                return result;
            });
        }

        private bool CompareEncodingCoarse(short[] pcm)
        {
            GcAdpcmChannel adpcmA = null;
            GcAdpcmChannel adpcmB = null;

            Parallel.Invoke(
                () => adpcmA = DllA.EncodeChannel(pcm),
                () => adpcmB = DllB.EncodeChannel(pcm)
            );

            return ArraysEqual(adpcmA.Coefs, adpcmB.Coefs) == -1 &&
                   ArraysEqual(adpcmA.GetAdpcmAudio(), adpcmB.GetAdpcmAudio()) == -1;
        }

        private static Result CompareEncodingFine(short[] pcm, IDspTool dllA, IDspTool dllB)
        {
            short[] coefsA = dllA.DspCorrelateCoefs(pcm);
            short[] coefsB = dllB.DspCorrelateCoefs(pcm);

            int coefsEqual = ArraysEqual(coefsA, coefsB);
            if (coefsEqual != -1)
            {
                return new Result
                {
                    Equal = false,
                    CoefsEqual = false,
                    CoefsA = coefsA,
                    CoefsB = coefsB
                };
            }

            int sampleCount = pcm.Length;
            var adpcmA = new byte[GcAdpcmHelpers.SampleCountToByteCount(sampleCount)];
            var adpcmB = new byte[GcAdpcmHelpers.SampleCountToByteCount(sampleCount)];

            /* Execute encoding-predictor for each frame */
            var pcmBufferA = new short[2 + GcAdpcmHelpers.SamplesPerFrame];
            var pcmBufferB = new short[2 + GcAdpcmHelpers.SamplesPerFrame];
            var adpcmBufferA = new byte[GcAdpcmHelpers.BytesPerFrame];
            var adpcmBufferB = new byte[GcAdpcmHelpers.BytesPerFrame];

            int frameCount = DivideByRoundUp(sampleCount, GcAdpcmHelpers.SamplesPerFrame);

            for (int frame = 0; frame < frameCount; frame++)
            {
                int samplesToCopy = Math.Min(sampleCount - frame * GcAdpcmHelpers.SamplesPerFrame, GcAdpcmHelpers.SamplesPerFrame);
                Array.Copy(pcm, frame * GcAdpcmHelpers.SamplesPerFrame, pcmBufferA, 2, samplesToCopy);
                Array.Copy(pcm, frame * GcAdpcmHelpers.SamplesPerFrame, pcmBufferB, 2, samplesToCopy);
                Array.Clear(pcmBufferA, 2 + samplesToCopy, GcAdpcmHelpers.SamplesPerFrame - samplesToCopy);
                Array.Clear(pcmBufferB, 2 + samplesToCopy, GcAdpcmHelpers.SamplesPerFrame - samplesToCopy);

                dllA.DspEncodeFrame(pcmBufferA, GcAdpcmHelpers.SamplesPerFrame, adpcmBufferA, coefsA);
                dllB.DspEncodeFrame(pcmBufferB, GcAdpcmHelpers.SamplesPerFrame, adpcmBufferB, coefsB);

                int encodeEqual = ArraysEqual(adpcmBufferA, adpcmBufferB);
                if (encodeEqual != -1)
                {
                    int differentSample = ArraysEqual(pcmBufferA, pcmBufferB) - 2;

                    //Get the input PCM that resulted in different encodings
                    short[] history = new short[2 + GcAdpcmHelpers.SamplesPerFrame];
                    Array.Copy(pcm, frame * GcAdpcmHelpers.SamplesPerFrame, history, 2, samplesToCopy);
                    Array.Copy(pcmBufferA, 0, history, 0, 2);

                    short[] pcmA = new short[GcAdpcmHelpers.SamplesPerFrame];
                    short[] pcmB = new short[GcAdpcmHelpers.SamplesPerFrame];
                    Array.Copy(pcmBufferA, 2, pcmA, 0, samplesToCopy);
                    Array.Copy(pcmBufferB, 2, pcmB, 0, samplesToCopy);

                    return new Result
                    {
                        Equal = false,
                        CoefsEqual = true,
                        CoefsA = coefsA,
                        CoefsB = coefsB,
                        Frame = frame,
                        FrameSample = differentSample,
                        Sample = frame * GcAdpcmHelpers.SamplesPerFrame + differentSample,
                        PcmIn = history,
                        PcmOutA = pcmA,
                        PcmOutB = pcmB,
                        AdpcmOutA = adpcmBufferA,
                        AdpcmOutB = adpcmBufferB
                    };
                }

                Array.Copy(adpcmBufferA, 0, adpcmA, frame * GcAdpcmHelpers.BytesPerFrame, GcAdpcmHelpers.SampleCountToByteCount(samplesToCopy));
                Array.Copy(adpcmBufferB, 0, adpcmB, frame * GcAdpcmHelpers.BytesPerFrame, GcAdpcmHelpers.SampleCountToByteCount(samplesToCopy));

                pcmBufferA[0] = pcmBufferA[14];
                pcmBufferA[1] = pcmBufferA[15];
                pcmBufferB[0] = pcmBufferB[14];
                pcmBufferB[1] = pcmBufferB[15];
            }

            Console.WriteLine("Encode Equal");
            return new Result { Equal = true };
        }

        private static int ArraysEqual<T>(T[] a1, T[] a2)
        {
            if (a1 == null || a2 == null) return -2;
            if (a1 == a2) return -1;
            if (a1.Length != a2.Length) return -3;

            for (int i = 0; i < a1.Length; i++)
            {
                if (!a1[i].Equals(a2[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        private static int DivideByRoundUp(int value, int divisor) => (int)Math.Ceiling((double)value / divisor);
    }
}
