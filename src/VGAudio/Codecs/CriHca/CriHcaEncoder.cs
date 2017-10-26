using System;
using System.Collections.Generic;
using System.IO;
using VGAudio.Utilities;
using static VGAudio.Codecs.CriHca.CriHcaConstants;
using static VGAudio.Codecs.CriHca.CriHcaPacking;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Codecs.CriHca
{
    public class CriHcaEncoder
    {
        private CriHcaEncoder() { }

        public HcaInfo Hca { get; private set; }
        public CriHcaQuality Quality { get; private set; }
        public int Bitrate { get; private set; }
        public int CutoffFrequency { get; private set; }
        /// <summary>
        /// The number of buffered frames waiting to be read.
        /// All buffered frames must be read before calling <see cref="Encode"/> again.
        /// </summary>
        public int PendingFrameCount => HcaOutputBuffer.Count;
        /// <summary>
        /// The size, in bytes, of one frame of HCA audio data.
        /// </summary>
        public int FrameSize => Hca.FrameSize;

        private CriHcaChannel[] Channels { get; set; }
        private CriHcaFrame Frame { get; set; }
        private Crc16 Crc { get; } = new Crc16(0x8005);

        private short[][] PcmBuffer { get; set; }
        private int BufferPosition { get; set; }
        private int BufferRemaining => SamplesPerFrame - BufferPosition;
        private int BufferPreSamples { get; set; }
        private int SamplesProcessed { get; set; }
        private int PostSamples { get; set; }
        private short[][] PostAudio { get; set; }

        private Queue<byte[]> HcaOutputBuffer { get; set; }

        /// <summary>
        /// Creates and initializes a new <see cref="CriHcaEncoder"/>. The encoder
        /// will be ready to accept PCM audio via <see cref="Encode"/>.
        /// </summary>
        /// <param name="config">The configuration to be used when creating the HCA file.</param>
        public static CriHcaEncoder InitializeNew(CriHcaParameters config)
        {
            var encoder = new CriHcaEncoder();
            encoder.Initialize(config);
            return encoder;
        }

        /// <summary>
        /// Initializes this <see cref="CriHcaEncoder"/>. Any preexisting state is reset, and the encoder
        /// will be ready to accept PCM audio via <see cref="Encode"/>.
        /// </summary>
        /// <param name="config">The configuration to be used when creating the HCA file.</param>
        public void Initialize(CriHcaParameters config)
        {
            CutoffFrequency = config.SampleRate / 2;
            Quality = config.Quality;
            PostSamples = 128;

            Hca = new HcaInfo
            {
                ChannelCount = config.ChannelCount,
                TrackCount = 1,
                SampleCount = config.SampleCount,
                SampleRate = config.SampleRate,
                MinResolution = 1,
                MaxResolution = 15,
                InsertedSamples = SamplesPerSubFrame
            };

            Bitrate = CalculateBitrate(Hca, Quality, config.Bitrate, config.LimitBitrate);
            CalculateBandCounts(Hca, Bitrate, CutoffFrequency);
            Hca.CalculateHfrValues();
            SetChannelConfiguration(Hca);

            int inputSampleCount = Hca.SampleCount;

            if (config.Looping)
            {
                Hca.Looping = true;
                Hca.SampleCount = Math.Min(config.LoopEnd, config.SampleCount);
                Hca.InsertedSamples += GetNextMultiple(config.LoopStart, SamplesPerFrame) - config.LoopStart;
                CalculateLoopInfo(Hca, config.LoopStart, config.LoopEnd);
                inputSampleCount = GetNextMultiple(Hca.SampleCount, SamplesPerSubFrame) + SamplesPerSubFrame * 2;
                PostSamples = inputSampleCount - Hca.SampleCount;
            }

            CalculateHeaderSize(Hca);

            int totalSamples = inputSampleCount + Hca.InsertedSamples;

            Hca.FrameCount = totalSamples.DivideByRoundUp(SamplesPerFrame);
            Hca.AppendedSamples = Hca.FrameCount * SamplesPerFrame - Hca.InsertedSamples - inputSampleCount;

            Frame = new CriHcaFrame(Hca);
            Channels = Frame.Channels;
            PcmBuffer = CreateJaggedArray<short[][]>(Hca.ChannelCount, SamplesPerFrame);
            PostAudio = CreateJaggedArray<short[][]>(Hca.ChannelCount, PostSamples);
            HcaOutputBuffer = new Queue<byte[]>();
            BufferPreSamples = Hca.InsertedSamples - 128;
        }

        /// <summary>
        /// Encodes one frame of PCM audio into the HCA format.
        /// </summary>
        /// <param name="pcm">The PCM audio to encode. The array must be a jagged array
        /// of the size [ChannelCount][1024] or larger.</param>
        /// <param name="hcaOut">The buffer that the encoded HCA frame will be placed in.
        /// Must be at least <see cref="FrameSize"/> bytes long.</param>
        /// <returns>The number of HCA frames that were output by the encoder.
        /// The first frame is output to <paramref name="hcaOut"/>. Any additional frames must be retrieved
        /// by calling <see cref="GetPendingFrame"/> before <see cref="Encode"/> can be called again.</returns>
        public int Encode(short[][] pcm, byte[] hcaOut)
        {
            int framesOutput = 0;
            int pcmPosition = 0;

            if (BufferPreSamples > 0)
            {
                framesOutput = EncodePreAudio(pcm, hcaOut, framesOutput);
            }

            if (Hca.Looping && Hca.LoopStartSample + PostSamples >= SamplesProcessed && Hca.LoopStartSample < SamplesProcessed + SamplesPerFrame)
            {
                SaveLoopAudio(pcm);
            }

            while (SamplesPerFrame - pcmPosition > 0 && Hca.SampleCount > SamplesProcessed)
            {
                framesOutput = EncodeMainAudio(pcm, hcaOut, framesOutput, ref pcmPosition);
            }

            if (Hca.SampleCount == SamplesProcessed)
            {
                framesOutput = EncodePostAudio(pcm, hcaOut, framesOutput);
            }

            return framesOutput;
        }

        /// <summary>
        /// Returns the next HCA frame awaiting output.
        /// </summary>
        /// <returns>Byte array containing the HCA frame data. The caller is given ownership of this array.</returns>
        /// <exception cref="InvalidOperationException">Thrown when there are no frames awaiting output.</exception>
        public byte[] GetPendingFrame()
        {
            if (PendingFrameCount == 0) throw new InvalidOperationException("There are no pending frames");

            return HcaOutputBuffer.Dequeue();
        }

        private int EncodePreAudio(short[][] pcm, byte[] hcaOut, int framesOutput)
        {
            while (BufferPreSamples > SamplesPerFrame)
            {
                BufferPosition = SamplesPerFrame;
                framesOutput = OutputFrame(framesOutput, hcaOut);
                BufferPreSamples -= SamplesPerFrame;
            }

            for (int j = 0; j < BufferPreSamples; j++)
            {
                for (int i = 0; i < pcm.Length; i++)
                {
                    PcmBuffer[i][j] = pcm[i][0];
                }
            }

            BufferPosition = BufferPreSamples;
            BufferPreSamples = 0;
            return framesOutput;
        }

        private int EncodeMainAudio(short[][] pcm, byte[] hcaOut, int framesOutput, ref int pcmPosition)
        {
            int toCopy = Math.Min(BufferRemaining, SamplesPerFrame - pcmPosition);
            toCopy = Math.Min(toCopy, Hca.SampleCount - SamplesProcessed);

            for (int i = 0; i < pcm.Length; i++)
            {
                Array.Copy(pcm[i], pcmPosition, PcmBuffer[i], BufferPosition, toCopy);
            }
            BufferPosition += toCopy;
            SamplesProcessed += toCopy;
            pcmPosition += toCopy;

            framesOutput = OutputFrame(framesOutput, hcaOut);
            return framesOutput;
        }

        private int EncodePostAudio(short[][] pcm, byte[] hcaOut, int framesOutput)
        {
            int postPos = 0;
            int remaining = PostSamples;

            while (postPos < remaining)
            {
                int toCopy = Math.Min(BufferRemaining, remaining - postPos);
                for (int i = 0; i < pcm.Length; i++)
                {
                    Array.Copy(PostAudio[i], postPos, PcmBuffer[i], BufferPosition, toCopy);
                }

                BufferPosition += toCopy;
                postPos += toCopy;

                framesOutput = OutputFrame(framesOutput, hcaOut);
            }

            if (BufferPosition == 0) return framesOutput;

            for (int i = 0; i < pcm.Length; i++)
            {
                Array.Clear(PcmBuffer[i], BufferPosition, BufferRemaining);
            }
            BufferPosition = SamplesPerFrame;

            framesOutput = OutputFrame(framesOutput, hcaOut);
            return framesOutput;
        }

        private void SaveLoopAudio(short[][] pcm)
        {
            int startPos = Math.Max(Hca.LoopStartSample - SamplesProcessed, 0);
            int loopPos = Math.Max(SamplesProcessed - Hca.LoopStartSample, 0);
            int endPos = Math.Min(Hca.LoopStartSample - SamplesProcessed + PostSamples, SamplesPerFrame);
            int length = endPos - startPos;
            for (int i = 0; i < pcm.Length; i++)
            {
                Array.Copy(pcm[i], startPos, PostAudio[i], loopPos, length);
            }
        }

        private int OutputFrame(int framesOutput, byte[] hcaOut)
        {
            if (BufferRemaining != 0) return framesOutput;

            byte[] hca = framesOutput == 0 ? hcaOut : new byte[Hca.FrameSize];
            EncodeFrame(PcmBuffer, hca);
            if (framesOutput > 0)
            {
                HcaOutputBuffer.Enqueue(hca);
            }
            BufferPosition = 0;
            return framesOutput + 1;
        }

        private void EncodeFrame(short[][] pcm, byte[] hcaOut)
        {
            PcmToFloat(pcm, Channels);
            RunMdct(Channels);
            EncodeIntensityStereo(Frame);
            CalculateScaleFactors(Channels);
            ScaleSpectra(Channels);
            CalculateHfrGroupAverages(Frame);
            CalculateHfrScale(Frame);
            CalculateFrameHeaderLength(Frame);
            CalculateNoiseLevel(Frame);
            CalculateEvaluationBoundary(Frame);
            CalculateFrameResolutions(Frame);
            QuantizeSpectra(Channels);
            PackFrame(Frame, Crc, hcaOut);
        }

        private int CalculateBitrate(HcaInfo hca, CriHcaQuality quality, int bitrate, bool limitBitrate)
        {
            int pcmBitrate = Hca.SampleRate * Hca.ChannelCount * 16;
            int maxBitrate = pcmBitrate / 4;
            int minBitrate = 0;

            int compressionRatio = 6;
            switch (quality)
            {
                case CriHcaQuality.Highest:
                    compressionRatio = 4;
                    break;
                case CriHcaQuality.High:
                    compressionRatio = 6;
                    break;
                case CriHcaQuality.Middle:
                    compressionRatio = 8;
                    break;
                case CriHcaQuality.Low:
                    compressionRatio = hca.ChannelCount == 1 ? 10 : 12;
                    break;
                case CriHcaQuality.Lowest:
                    compressionRatio = hca.ChannelCount == 1 ? 12 : 16;
                    break;
            }

            bitrate = bitrate != 0 ? bitrate : pcmBitrate / compressionRatio;

            if (limitBitrate)
            {
                minBitrate = Math.Min(
                    hca.ChannelCount == 1 ? 42666 : 32000 * hca.ChannelCount,
                    pcmBitrate / 6);
            }

            return Clamp(bitrate, minBitrate, maxBitrate);
        }

        private static void CalculateBandCounts(HcaInfo hca, int bitrate, int cutoffFreq)
        {
            hca.FrameSize = bitrate * 1024 / hca.SampleRate / 8;
            int numGroups = 0;
            int pcmBitrate = hca.SampleRate * hca.ChannelCount * 16;
            int hfrRatio; // HFR is used at bitrates below (pcmBitrate / hfrRatio)
            int cutoffRatio; // The cutoff frequency is lowered at bitrates below (pcmBitrate / cutoffRatio)

            if (hca.ChannelCount <= 1 || pcmBitrate / bitrate <= 6)
            {
                hfrRatio = 6;
                cutoffRatio = 12;
            }
            else
            {
                hfrRatio = 8;
                cutoffRatio = 16;
            }

            if (bitrate < pcmBitrate / cutoffRatio)
            {
                cutoffFreq = Math.Min(cutoffFreq, cutoffRatio * bitrate / (32 * hca.ChannelCount));
            }

            int totalBandCount = (int)Math.Round(cutoffFreq * 256d / hca.SampleRate);

            int hfrStartBand = (int)Math.Min(totalBandCount, Math.Round((hfrRatio * bitrate * 128d) / pcmBitrate));
            int stereoStartBand = hfrRatio == 6 ? hfrStartBand : (hfrStartBand + 1) / 2;

            int hfrBandCount = totalBandCount - hfrStartBand;
            int bandsPerGroup = hfrBandCount.DivideByRoundUp(8);

            if (bandsPerGroup > 0)
            {
                numGroups = hfrBandCount.DivideByRoundUp(bandsPerGroup);
            }

            hca.TotalBandCount = totalBandCount;
            hca.BaseBandCount = stereoStartBand;
            hca.StereoBandCount = hfrStartBand - stereoStartBand;
            hca.HfrGroupCount = numGroups;
            hca.BandsPerHfrGroup = bandsPerGroup;
        }

        private static void SetChannelConfiguration(HcaInfo hca, int channelConfig = -1)
        {
            int channelsPerTrack = hca.ChannelCount / hca.TrackCount;
            if (channelConfig == -1) channelConfig = CriHcaTables.DefaultChannelMapping[channelsPerTrack];

            if (CriHcaTables.ValidChannelMappings[channelsPerTrack - 1][channelConfig] != 1)
            {
                throw new ArgumentOutOfRangeException(nameof(channelConfig), "Channel mapping is not valid.");
            }

            hca.ChannelConfig = channelConfig;
        }

        private static void CalculateLoopInfo(HcaInfo hca, int loopStart, int loopEnd)
        {
            loopStart += hca.InsertedSamples;
            loopEnd += hca.InsertedSamples;

            hca.LoopStartFrame = loopStart / SamplesPerFrame;
            hca.PreLoopSamples = loopStart % SamplesPerFrame;
            hca.LoopEndFrame = loopEnd / SamplesPerFrame;
            hca.PostLoopSamples = SamplesPerFrame - loopEnd % SamplesPerFrame;

            if (hca.PostLoopSamples == SamplesPerFrame)
            {
                hca.LoopEndFrame--;
                hca.PostLoopSamples = 0;
            }
        }

        private static void CalculateHeaderSize(HcaInfo hca)
        {
            const int baseHeaderSize = 96;
            const int baseHeaderAlignment = 32;
            const int loopFrameAlignment = 2048;

            hca.HeaderSize = GetNextMultiple(baseHeaderSize + hca.CommentLength, baseHeaderAlignment);
            if (hca.Looping)
            {
                int loopFrameOffset = hca.HeaderSize + hca.FrameSize * hca.LoopStartFrame;
                int paddingBytes = GetNextMultiple(loopFrameOffset, loopFrameAlignment) - loopFrameOffset;
                int paddingFrames = paddingBytes / hca.FrameSize;

                hca.InsertedSamples += paddingFrames * SamplesPerFrame;
                hca.LoopStartFrame += paddingFrames;
                hca.LoopEndFrame += paddingFrames;
                hca.HeaderSize += paddingBytes % hca.FrameSize;
            }
        }

        private static void QuantizeSpectra(CriHcaChannel[] channels)
        {
            foreach (CriHcaChannel channel in channels)
            {
                for (int i = 0; i < channel.CodedScaleFactorCount; i++)
                {
                    var scaled = channel.ScaledSpectra[i];
                    int resolution = channel.Resolution[i];
                    for (int sf = 0; sf < scaled.Length; sf++)
                    {
                        double value = scaled[sf] + 1;
                        channel.QuantizedSpectra[sf][i] = (int)(value * CriHcaTables.QuantizerRangeTable[resolution]) -
                                                          CriHcaTables.ResolutionMaxValues[resolution];
                    }
                }
            }
        }

        private static void CalculateFrameResolutions(CriHcaFrame frame)
        {
            foreach (CriHcaChannel channel in frame.Channels)
            {
                for (int i = 0; i < frame.EvaluationBoundary; i++)
                {
                    channel.Resolution[i] = CalculateResolution(channel.ScaleFactors[i], frame.AcceptableNoiseLevel - 1);
                }
                for (int i = frame.EvaluationBoundary; i < channel.CodedScaleFactorCount; i++)
                {
                    channel.Resolution[i] = CalculateResolution(channel.ScaleFactors[i], frame.AcceptableNoiseLevel);
                }
                Array.Clear(channel.Resolution, channel.CodedScaleFactorCount, channel.Resolution.Length - channel.CodedScaleFactorCount);
            }
        }

        private static void CalculateNoiseLevel(CriHcaFrame frame)
        {
            int highestBand = frame.Hca.BaseBandCount + frame.Hca.StereoBandCount - 1;
            int availableBits = frame.Hca.FrameSize * 8;
            int maxLevel = 255;
            int minLevel = 0;
            int level = BinarySearchLevel(frame.Channels, availableBits, minLevel, maxLevel);

            // If there aren't enough available bits, remove bands until there are.
            while (level < 0)
            {
                highestBand -= 2;
                if (highestBand < 0)
                {
                    throw new InvalidDataException("Bitrate is set too low.");
                }

                foreach (CriHcaChannel channel in frame.Channels)
                {
                    channel.ScaleFactors[highestBand + 1] = 0;
                    channel.ScaleFactors[highestBand + 2] = 0;
                }

                CalculateFrameHeaderLength(frame);
                level = BinarySearchLevel(frame.Channels, availableBits, minLevel, maxLevel);
            }

            frame.AcceptableNoiseLevel = level;
        }

        private static void CalculateEvaluationBoundary(CriHcaFrame frame)
        {
            if (frame.AcceptableNoiseLevel == 0)
            {
                frame.EvaluationBoundary = 0;
                return;
            }

            int availableBits = frame.Hca.FrameSize * 8;
            int maxLevel = 127;
            int minLevel = 0;
            int level = BinarySearchBoundary(frame.Channels, availableBits, frame.AcceptableNoiseLevel, minLevel, maxLevel);
            frame.EvaluationBoundary = level >= 0 ? level : throw new NotImplementedException();
        }

        private static int BinarySearchLevel(CriHcaChannel[] channels, int availableBits, int low, int high)
        {
            int max = high;
            int midValue = 0;

            while (low != high)
            {
                int mid = (low + high) / 2;
                midValue = CalculateUsedBits(channels, mid, 0);

                if (midValue > availableBits)
                {
                    low = mid + 1;
                }
                else if (midValue <= availableBits)
                {
                    high = mid;
                }
            }

            return low == max && midValue > availableBits ? -1 : low;
        }

        private static int BinarySearchBoundary(CriHcaChannel[] channels, int availableBits, int noiseLevel, int low, int high)
        {
            int max = high;

            while (Math.Abs(high - low) > 1)
            {
                int mid = (low + high) / 2;
                int midValue = CalculateUsedBits(channels, noiseLevel, mid);

                if (availableBits < midValue)
                {
                    high = mid - 1;
                }
                else if (availableBits >= midValue)
                {
                    low = mid;
                }
            }

            if (low == high)
            {
                return low < max ? low : -1;
            }

            int hiValue = CalculateUsedBits(channels, noiseLevel, high);

            return hiValue > availableBits ? low : high;
        }

        private static int CalculateUsedBits(CriHcaChannel[] channels, int noiseLevel, int evalBoundary)
        {
            int length = 16 + 16 + 16; // Sync word, noise level and checksum

            foreach (CriHcaChannel channel in channels)
            {
                length += channel.HeaderLengthBits;
                for (int i = 0; i < channel.CodedScaleFactorCount; i++)
                {
                    int noise = i < evalBoundary ? noiseLevel - 1 : noiseLevel;
                    int resolution = CalculateResolution(channel.ScaleFactors[i], noise);

                    foreach (double scaledSpectra in channel.ScaledSpectra[i])
                    {
                        double value = scaledSpectra + 1;
                        int quantizedSpectra = (int)(value * CriHcaTables.QuantizerRangeTable[resolution]) -
                                               CriHcaTables.ResolutionMaxValues[resolution];
                        length += CalculateBitsUsedBySpectra(quantizedSpectra, resolution);
                    }
                }
            }

            return length;
        }

        private static int CalculateBitsUsedBySpectra(int quantizedSpectra, int resolution)
        {
            if (resolution >= 8)
            {
                return CriHcaTables.QuantizedSpectrumMaxBits[resolution] - (quantizedSpectra == 0 ? 1 : 0);
            }
            return CriHcaTables.QuantizeSpectrumBits[resolution][quantizedSpectra + 8];
        }

        private static void CalculateFrameHeaderLength(CriHcaFrame frame)
        {
            foreach (CriHcaChannel channel in frame.Channels)
            {
                CalculateOptimalDeltaLength(channel);
                if (channel.Type == ChannelType.StereoSecondary) channel.HeaderLengthBits += 32;
                else if (frame.Hca.HfrGroupCount > 0) channel.HeaderLengthBits += 6 * frame.Hca.HfrGroupCount;
            }
        }

        private static void CalculateOptimalDeltaLength(CriHcaChannel channel)
        {
            bool emptyChannel = true;
            for (int i = 0; i < channel.CodedScaleFactorCount; i++)
            {
                if (channel.ScaleFactors[i] != 0)
                {
                    emptyChannel = false;
                    break;
                }
            }

            if (emptyChannel)
            {
                channel.HeaderLengthBits = 3;
                channel.ScaleFactorDeltaBits = 0;
                return;
            }

            int minDeltaBits = 6;
            int minLength = 3 + 6 * channel.CodedScaleFactorCount;

            for (int deltaBits = 1; deltaBits < 6; deltaBits++)
            {
                int maxDelta = (1 << (deltaBits - 1)) - 1;
                int length = 3 + 6;
                for (int band = 1; band < channel.CodedScaleFactorCount; band++)
                {
                    int delta = channel.ScaleFactors[band] - channel.ScaleFactors[band - 1];
                    length += Math.Abs(delta) > maxDelta ? deltaBits + 6 : deltaBits;
                }
                if (length < minLength)
                {
                    minLength = length;
                    minDeltaBits = deltaBits;
                }
            }

            channel.HeaderLengthBits = minLength;
            channel.ScaleFactorDeltaBits = minDeltaBits;
        }

        private static void ScaleSpectra(CriHcaChannel[] channels)
        {
            foreach (CriHcaChannel channel in channels)
            {
                for (int b = 0; b < channel.CodedScaleFactorCount; b++)
                {
                    double[] scaledSpectra = channel.ScaledSpectra[b];
                    int scaleFactor = channel.ScaleFactors[b];
                    for (int sf = 0; sf < scaledSpectra.Length; sf++)
                    {
                        double coeff = channel.Spectra[sf][b];
                        scaledSpectra[sf] = scaleFactor == 0 ? 0 :
                            coeff * CriHcaTables.QuantizerScalingTable[scaleFactor];
                    }
                }
            }
        }

        private static void CalculateScaleFactors(CriHcaChannel[] channels)
        {
            foreach (CriHcaChannel channel in channels)
            {
                for (int b = 0; b < channel.CodedScaleFactorCount; b++)
                {
                    double max = 0;
                    for (int sf = 0; sf < SubframesPerFrame; sf++)
                    {
                        var coeff = Math.Abs(channel.Spectra[sf][b]);
                        max = Math.Max(coeff, max);
                    }
                    channel.ScaleFactors[b] = FindScaleFactor(max);
                }
                Array.Clear(channel.ScaleFactors, channel.CodedScaleFactorCount, channel.ScaleFactors.Length - channel.CodedScaleFactorCount);
            }
        }

        private static int FindScaleFactor(double value)
        {
            float[] sf = CriHcaTables.DequantizerScalingTable;
            for (int i = 0; i < sf.Length; i++)
            {
                if (sf[i] > value) return i;
            }
            return 63;
        }

        private static void EncodeIntensityStereo(CriHcaFrame frame)
        {
            if (frame.Hca.StereoBandCount <= 0) return;

            for (int c = 0; c < frame.Channels.Length; c++)
            {
                if (frame.Channels[c].Type != ChannelType.StereoPrimary) continue;

                for (int sf = 0; sf < SubframesPerFrame; sf++)
                {
                    double[] l = frame.Channels[c].Spectra[sf];
                    double[] r = frame.Channels[c + 1].Spectra[sf];

                    double energyL = 0;
                    double energyR = 0;
                    double energyTotal = 0;

                    for (int b = frame.Hca.BaseBandCount; b < frame.Hca.TotalBandCount; b++)
                    {
                        energyL += Math.Abs(l[b]);
                        energyR += Math.Abs(r[b]);
                        energyTotal += Math.Abs(l[b] + r[b]);
                    }
                    energyTotal *= 2;

                    double energyLR = energyR + energyL;
                    double storedValue = 2 * energyL / energyLR;
                    double energyRatio = energyLR / energyTotal;
                    energyRatio = Clamp(energyRatio, 0.5, Math.Sqrt(2) / 2);

                    int quantized = 1;
                    if (energyR > 0 || energyL > 0)
                    {
                        while (quantized < 13 && CriHcaTables.IntensityRatioBoundsTable[quantized] >= storedValue)
                        {
                            quantized++;
                        }
                    }
                    else
                    {
                        quantized = 0;
                        energyRatio = 1;
                    }

                    frame.Channels[c + 1].Intensity[sf] = quantized;

                    for (int b = frame.Hca.BaseBandCount; b < frame.Hca.TotalBandCount; b++)
                    {
                        l[b] = (l[b] + r[b]) * energyRatio;
                        r[b] = 0;
                    }
                }
            }
        }

        private static void CalculateHfrGroupAverages(CriHcaFrame frame)
        {
            HcaInfo hca = frame.Hca;
            if (hca.HfrGroupCount == 0) return;

            int hfrStartBand = hca.StereoBandCount + hca.BaseBandCount;
            foreach (CriHcaChannel channel in frame.Channels)
            {
                if (channel.Type == ChannelType.StereoSecondary) continue;

                for (int group = 0, band = hfrStartBand; group < hca.HfrGroupCount; group++)
                {
                    double sum = 0.0;
                    int count = 0;

                    for (int i = 0; i < hca.BandsPerHfrGroup && band < SamplesPerSubFrame; band++, i++)
                    {
                        for (int subframe = 0; subframe < SubframesPerFrame; subframe++)
                        {
                            sum += Math.Abs(channel.Spectra[subframe][band]);
                        }
                        count += SubframesPerFrame;
                    }

                    channel.HfrGroupAverageSpectra[group] = sum / count;
                }
            }
        }

        private static void CalculateHfrScale(CriHcaFrame frame)
        {
            HcaInfo hca = frame.Hca;
            if (hca.HfrGroupCount == 0) return;

            int hfrStartBand = hca.StereoBandCount + hca.BaseBandCount;
            int hfrBandCount = Math.Min(hca.HfrBandCount, hca.TotalBandCount - hca.HfrBandCount);

            foreach (CriHcaChannel channel in frame.Channels)
            {
                if (channel.Type == ChannelType.StereoSecondary) continue;

                double[] groupSpectra = channel.HfrGroupAverageSpectra;

                for (int group = 0, band = 0; group < hca.HfrGroupCount; group++)
                {
                    double sum = 0.0;
                    int count = 0;

                    for (int i = 0; i < hca.BandsPerHfrGroup && band < hfrBandCount; band++, i++)
                    {
                        for (int subframe = 0; subframe < SubframesPerFrame; subframe++)
                        {
                            sum += Math.Abs(channel.ScaledSpectra[hfrStartBand - band - 1][subframe]);
                        }
                        count += SubframesPerFrame;
                    }

                    double averageSpectra = sum / count;
                    if (averageSpectra > 0.0)
                    {
                        groupSpectra[group] *= Math.Min(1.0 / averageSpectra, Math.Sqrt(2));
                    }

                    channel.HfrScales[group] = FindScaleFactor(groupSpectra[group]);
                }
            }
        }

        private static void RunMdct(CriHcaChannel[] channels)
        {
            foreach (CriHcaChannel channel in channels)
            {
                for (int sf = 0; sf < SubframesPerFrame; sf++)
                {
                    channel.Mdct.RunMdct(channel.PcmFloat[sf], channel.Spectra[sf]);
                }
            }
        }

        private static void PcmToFloat(short[][] pcm, CriHcaChannel[] channels)
        {
            for (int c = 0; c < channels.Length; c++)
            {
                int pcmIdx = 0;
                for (int sf = 0; sf < SubframesPerFrame; sf++)
                {
                    for (int i = 0; i < SamplesPerSubFrame; i++)
                    {
                        channels[c].PcmFloat[sf][i] = pcm[c][pcmIdx++] * (1f / 32768f);
                    }
                }
            }
        }
    }
}
