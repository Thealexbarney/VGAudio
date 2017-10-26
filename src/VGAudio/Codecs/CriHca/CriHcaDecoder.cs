using System;
using VGAudio.Utilities;
using static VGAudio.Codecs.CriHca.CriHcaConstants;
using static VGAudio.Codecs.CriHca.CriHcaPacking;
using static VGAudio.Codecs.CriHca.CriHcaTables;

namespace VGAudio.Codecs.CriHca
{
    public static class CriHcaDecoder
    {
        public static short[][] Decode(HcaInfo hca, byte[][] audio, CriHcaParameters config = null)
        {
            config?.Progress?.SetTotal(hca.FrameCount);
            var pcmOut = Helpers.CreateJaggedArray<short[][]>(hca.ChannelCount, hca.SampleCount);
            var pcmBuffer = Helpers.CreateJaggedArray<short[][]>(hca.ChannelCount, SamplesPerFrame);

            var frame = new CriHcaFrame(hca);

            for (int i = 0; i < hca.FrameCount; i++)
            {
                DecodeFrame(audio[i], frame, pcmBuffer);

                CopyPcmToOutput(pcmBuffer, pcmOut, hca, i);
                //CopyBuffer(pcmBuffer, pcmOut, hca.InsertedSamples, i);
                config?.Progress?.ReportAdd(1);
            }

            return pcmOut;
        }

        private static void CopyPcmToOutput(short[][] pcmIn, short[][] pcmOut, HcaInfo hca, int frame)
        {
            int currentSample = frame * SamplesPerFrame - hca.InsertedSamples;
            int remainingSamples = Math.Min(hca.SampleCount - currentSample, hca.SampleCount);
            int srcStart = Helpers.Clamp(0 - currentSample, 0, SamplesPerFrame);
            int destStart = Math.Max(currentSample, 0);

            int length = Math.Min(SamplesPerFrame - srcStart, remainingSamples);
            if (length <= 0) return;

            for (int c = 0; c < pcmOut.Length; c++)
            {
                Array.Copy(pcmIn[c], srcStart, pcmOut[c], destStart, length);
            }
        }

        public static void CopyBuffer(short[][] bufferIn, short[][] bufferOut, int startIndex, int bufferIndex)
        {
            if (bufferIn == null || bufferOut == null || bufferIn.Length == 0 || bufferOut.Length == 0)
            {
                throw new ArgumentException(
                    $"{nameof(bufferIn)} and {nameof(bufferOut)} must be non-null with a length greater than 0");
            }

            int bufferLength = bufferIn[0].Length;
            int outLength = bufferOut[0].Length;

            int currentIndex = bufferIndex * bufferLength - startIndex;
            int remainingElements = Math.Min(outLength - currentIndex, outLength);
            int srcStart = Helpers.Clamp(0 - currentIndex, 0, SamplesPerFrame);
            int destStart = Math.Max(currentIndex, 0);

            int length = Math.Min(SamplesPerFrame - srcStart, remainingElements);
            if (length <= 0) return;

            for (int c = 0; c < bufferOut.Length; c++)
            {
                Array.Copy(bufferIn[c], srcStart, bufferOut[c], destStart, length);
            }
        }

        private static void DecodeFrame(byte[] audio, CriHcaFrame frame, short[][] pcmOut)
        {
            var reader = new BitReader(audio);

            UnpackFrame(frame, reader);
            DequantizeFrame(frame);
            RestoreMissingBands(frame);
            RunImdct(frame);
            PcmFloatToShort(frame, pcmOut);
        }

        private static void DequantizeFrame(CriHcaFrame frame)
        {
            foreach (CriHcaChannel channel in frame.Channels)
            {
                CalculateGain(channel);
            }

            for (int sf = 0; sf < SubframesPerFrame; sf++)
            {
                foreach (CriHcaChannel channel in frame.Channels)
                {
                    for (int s = 0; s < channel.CodedScaleFactorCount; s++)
                    {
                        channel.Spectra[sf][s] = channel.QuantizedSpectra[sf][s] * channel.Gain[s];
                    }
                }
            }
        }

        private static void RestoreMissingBands(CriHcaFrame frame)
        {
            ReconstructHighFrequency(frame);
            ApplyIntensityStereo(frame);
        }

        private static void CalculateGain(CriHcaChannel channel)
        {
            for (int i = 0; i < channel.CodedScaleFactorCount; i++)
            {
                channel.Gain[i] = DequantizerScalingTable[channel.ScaleFactors[i]] * DequantizerRangeTable[channel.Resolution[i]];
            }
        }

        private static void ReconstructHighFrequency(CriHcaFrame frame)
        {
            HcaInfo hca = frame.Hca;
            if (hca.HfrGroupCount == 0) return;

            // The last spectral coefficient should always be 0;
            int totalBandCount = Math.Min(hca.TotalBandCount, 127);

            int hfrStartBand = hca.BaseBandCount + hca.StereoBandCount;
            int hfrBandCount = Math.Min(hca.HfrBandCount, totalBandCount - hca.HfrBandCount);

            foreach (CriHcaChannel channel in frame.Channels)
            {
                if (channel.Type == ChannelType.StereoSecondary) continue;

                for (int group = 0, band = 0; group < hca.HfrGroupCount; group++)
                {
                    for (int i = 0; i < hca.BandsPerHfrGroup && band < hfrBandCount; band++, i++)
                    {
                        int highBand = hfrStartBand + band;
                        int lowBand = hfrStartBand - band - 1;
                        int index = channel.HfrScales[group] - channel.ScaleFactors[lowBand] + 64;
                        for (int sf = 0; sf < SubframesPerFrame; sf++)
                        {
                            channel.Spectra[sf][highBand] = ScaleConversionTable[index] * channel.Spectra[sf][lowBand];
                        }
                    }
                }
            }
        }

        private static void ApplyIntensityStereo(CriHcaFrame frame)
        {
            if (frame.Hca.StereoBandCount <= 0) return;
            for (int c = 0; c < frame.Channels.Length; c++)
            {
                if (frame.Channels[c].Type != ChannelType.StereoPrimary) continue;
                for (int sf = 0; sf < SubframesPerFrame; sf++)
                {
                    double[] l = frame.Channels[c].Spectra[sf];
                    double[] r = frame.Channels[c + 1].Spectra[sf];
                    float ratioL = IntensityRatioTable[frame.Channels[c + 1].Intensity[sf]];
                    float ratioR = ratioL - 2.0f;
                    for (int b = frame.Hca.BaseBandCount; b < frame.Hca.TotalBandCount; b++)
                    {
                        r[b] = l[b] * ratioR;
                        l[b] *= ratioL;
                    }
                }
            }
        }

        private static void RunImdct(CriHcaFrame frame)
        {
            for (int sf = 0; sf < SubframesPerFrame; sf++)
            {
                foreach (CriHcaChannel channel in frame.Channels)
                {
                    channel.Mdct.RunImdct(channel.Spectra[sf], channel.PcmFloat[sf]);
                }
            }
        }

        private static void PcmFloatToShort(CriHcaFrame frame, short[][] pcm)
        {
            for (int c = 0; c < frame.Channels.Length; c++)
            {
                for (int sf = 0; sf < SubframesPerFrame; sf++)
                {
                    for (int s = 0; s < SamplesPerSubFrame; s++)
                    {
                        int sample = (int)(frame.Channels[c].PcmFloat[sf][s] * (short.MaxValue + 1));
                        pcm[c][sf * SamplesPerSubFrame + s] = Helpers.Clamp16(sample);
                    }
                }
            }
        }
    }
}
