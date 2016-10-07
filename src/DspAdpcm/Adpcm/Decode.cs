using System;
using System.Collections.Generic;
using System.Linq;
using static DspAdpcm.Helpers;
using DspAdpcm.Pcm;

#if !NOPARALLEL
using System.Threading.Tasks;
#endif

namespace DspAdpcm.Adpcm
{
    /// <summary>
    /// This class contains functions used for decoding
    /// Nintendo's 4-bit ADPCM audio format.
    /// </summary>
    public static class Decode
    {
        /// <summary>
        /// Decodes an <see cref="AdpcmStream"/> to a <see cref="PcmStream"/>.
        /// </summary>
        /// <param name="adpcmStream">The <see cref="AdpcmStream"/> to decode.</param>
        /// <returns>The decoded <see cref="PcmStream"/>.</returns>
        public static PcmStream AdpcmtoPcm(AdpcmStream adpcmStream)
        {
            PcmStream pcm = new PcmStream(adpcmStream.NumSamples, adpcmStream.SampleRate);
            var channels = new PcmChannel[adpcmStream.Channels.Count];

            for (int i = 0; i < channels.Length; i++)
            {
                channels[i] = AdpcmtoPcm(adpcmStream.Channels[i]);
            }

            foreach (PcmChannel channel in channels)
            {
                pcm.Channels.Add(channel);
            }

            return pcm;
        }

#if !NOPARALLEL
        /// <summary>
        /// Decodes an <see cref="AdpcmStream"/> to a <see cref="PcmStream"/>.
        /// Each channel will be decoded in parallel.
        /// </summary>
        /// <param name="adpcmStream">The <see cref="AdpcmStream"/> to decode.</param>
        /// <returns>The decoded <see cref="PcmStream"/>.</returns>
        public static PcmStream AdpcmtoPcmParallel(AdpcmStream adpcmStream)
        {
            PcmStream pcm = new PcmStream(adpcmStream.NumSamples, adpcmStream.SampleRate);
            var channels = new PcmChannel[adpcmStream.Channels.Count];

            Parallel.For(0, channels.Length, i =>
            {
                channels[i] = AdpcmtoPcm(adpcmStream.Channels[i]);
            });

            foreach (PcmChannel channel in channels)
            {
                pcm.Channels.Add(channel);
            }

            return pcm;
        }
#endif

        private static PcmChannel AdpcmtoPcm(AdpcmChannel adpcmChannel)
        {
            return new PcmChannel(adpcmChannel.NumSamples, adpcmChannel.GetPcmAudio());
        }

        internal static short[] GetPcmAudioLooped(this AdpcmChannel audio, int index, int count, int startLoop, int endLoop,
            bool includeHistorySamples = false)
        {
            short[] output = new short[count + (includeHistorySamples ? 2 : 0)];
            int outputIndex = 0;
            int samplesRemaining = count;
            bool firstTime = true;

            while (samplesRemaining > 0 || firstTime && includeHistorySamples)
            {
                int samplesToGet = Math.Min(endLoop - index, samplesRemaining);
                short[] samples = audio.GetPcmAudio(index, samplesToGet, firstTime && includeHistorySamples);
                Array.Copy(samples, 0, output, outputIndex, samples.Length);
                samplesRemaining -= samplesToGet;
                outputIndex += samples.Length;
                index = startLoop;
                firstTime = false;
            }

            return output;
        }

        internal static short[] GetPcmAudio(this AdpcmChannel audio, bool includeHistorySamples = false) =>
            audio.GetPcmAudio(0, audio.NumSamples, includeHistorySamples);

        internal static short[] GetPcmAudio(this AdpcmChannel audio, int index, int count, bool includeHistorySamples = false)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Argument must be non-negative");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "Argument must be non-negative");
            }

            if (audio.NumSamples - index < count)
            {
                throw new ArgumentException("Offset and length were out of bounds for the array or count is" +
                                            " greater than the number of elements from index to the end of the source collection.");
            }

            var history = audio.GetStartingHistory(index);

            short hist1 = history.Item2;
            short hist2 = history.Item3;
            var adpcm = audio.GetAudioData;
            int numFrames = adpcm.Length.DivideByRoundUp(BytesPerFrame);

            short[] pcm;
            int numHistSamples = 0;
            int currentSample = history.Item1;
            int outSample = 0;
            int inByte = currentSample / SamplesPerFrame * BytesPerFrame;

            if (includeHistorySamples)
            {
                numHistSamples = 2;
                pcm = new short[count + numHistSamples];
                if (index <= currentSample)
                {
                    pcm[outSample++] = hist2;
                }
                if (index <= currentSample + 1)
                {
                    pcm[outSample++] = hist1;
                }

            }
            else
            {
                pcm = new short[count];
            }

            int firstSample = Math.Max(index - numHistSamples, currentSample);
            int lastSample = index + count;

            if (firstSample == lastSample)
            {
                return pcm;
            }

            for (int i = 0; i < numFrames; i++)
            {
                byte ps = adpcm[inByte++];
                int scale = 1 << (ps & 0xf);
                int predictor = (ps >> 4) & 0xf;
                short coef1 = audio.Coefs[predictor * 2];
                short coef2 = audio.Coefs[predictor * 2 + 1];

                for (int s = 0; s < 14; s++)
                {
                    int sample;
                    if (s % 2 == 0)
                    {
                        sample = (adpcm[inByte] >> 4) & 0xF;
                    }
                    else
                    {
                        sample = adpcm[inByte++] & 0xF;
                    }
                    sample = sample >= 8 ? sample - 16 : sample;

                    sample = (((scale * sample) << 11) + 1024 + (coef1 * hist1 + coef2 * hist2)) >> 11;
                    sample = Clamp16(sample);

                    hist2 = hist1;
                    hist1 = (short)sample;

                    if (currentSample >= firstSample)
                    {
                        pcm[outSample++] = (short)sample;
                    }

                    if (++currentSample >= lastSample)
                    {
                        return pcm;
                    }
                }
            }
            return pcm;
        }

        private static Tuple<int, short, short> GetStartingHistory(this AdpcmChannel audio, int firstSample)
        {
            if (audio.SeekTable == null || !audio.SelfCalculatedSeekTable)
            {
                return new Tuple<int, short, short>(0, audio.Hist1, audio.Hist2);
            }

            int entry = firstSample / audio.SamplesPerSeekTableEntry;
            while (entry * 2 + 1 > audio.SeekTable.Length)
                entry--;

            int sample = entry * audio.SamplesPerSeekTableEntry;
            short hist1 = audio.SeekTable[entry * 2];
            short hist2 = audio.SeekTable[entry * 2 + 1];

            return new Tuple<int, short, short>(sample, hist1, hist2);
        }

        private static byte GetPredictorScale(this AdpcmChannel audio, int sample)
        {
            return audio.GetAudioData[sample / SamplesPerFrame * BytesPerFrame];
        }

        internal static void CalculateLoopContext(IEnumerable<AdpcmChannel> channels, int loopStart)
        {
#if !NOPARALLEL
            Parallel.ForEach(channels, channel => CalculateLoopContext(channel, loopStart));
#else
            foreach (AdpcmChannel channel in channels)
            {
                CalculateLoopContext(channel, loopStart);
            }
#endif
        }

        internal static void CalculateLoopContext(this AdpcmChannel audio, int loopStart)
        {
            byte ps = audio.GetPredictorScale(loopStart);
            short[] hist = audio.GetPcmAudio(loopStart, 0, true);
            audio.SetLoopContext(ps, hist[1], hist[0]);
            audio.SelfCalculatedLoopContext = true;
        }

        internal static void CalculateSeekTable(AdpcmChannel channel, int samplesPerEntry)
        {
            var audio = channel.GetPcmAudio(true);
            int numEntries = channel.NumSamples.DivideByRoundUp(samplesPerEntry);
            short[] table = new short[numEntries * 2];

            for (int i = 0; i < numEntries; i++)
            {
                table[i * 2] = audio[i * samplesPerEntry + 1];
                table[i * 2 + 1] = audio[i * samplesPerEntry];
            }

            channel.SeekTable = table;
            channel.SelfCalculatedSeekTable = true;
            channel.SamplesPerSeekTableEntry = samplesPerEntry;
        }

        internal static void CalculateSeekTable(IEnumerable<AdpcmChannel> channels, int samplesPerEntry)
        {
#if !NOPARALLEL
            Parallel.ForEach(channels, channel => CalculateSeekTable(channel, samplesPerEntry));
#else
            foreach (AdpcmChannel channel in channels)
            {
                CalculateSeekTable(channel, samplesPerEntry);
            }
#endif
        }

        internal static byte[] BuildSeekTable(IEnumerable<AdpcmChannel> channels, int samplesPerEntry, int numEntries, Endianness endianness)
        {
            channels = channels.ToList();
            CalculateSeekTable(channels.Where(x =>
            x.SeekTable == null || x.SamplesPerSeekTableEntry != samplesPerEntry), samplesPerEntry);

            var table = channels
                .Select(x => x.SeekTable)
                .ToArray()
                .Interleave(2);

            Array.Resize(ref table, numEntries * 2 * channels.Count());
            return table.ToByteArray(endianness);
        }

        internal static void CalculateLoopAlignment(IEnumerable<AdpcmChannel> channels, int alignment, int loopStart, int loopEnd)
        {
#if !NOPARALLEL
            Parallel.ForEach(channels, channel => CalculateLoopAlignment(channel, alignment, loopStart, loopEnd));
#else
            foreach (AdpcmChannel channel in channels)
            {
                CalculateLoopAlignment(channel, alignment, loopStart, loopEnd);
            }
#endif
        }

        internal static void CalculateLoopAlignment(this AdpcmChannel audio, int alignment, int loopStart, int loopEnd)
        {
            if (loopStart % alignment == 0)
            {
                audio.AudioByteArrayAligned = null;
                audio.LoopAlignment = alignment;
                audio.LoopStartAligned = 0;
                audio.LoopEndAligned = 0;
                return;
            }

            if (audio.LoopAlignment == alignment
                && audio.LoopStartAligned == loopStart
                && audio.LoopEndAligned == loopEnd)
            {
                return;
            }

            int outLoopStart = GetNextMultiple(loopStart, alignment);
            int samplesToAdd = outLoopStart - loopStart;
            int outputLength = GetBytesForAdpcmSamples(audio.NumSamples + samplesToAdd);
            var output = new byte[outputLength];

            int framesToCopy = loopEnd / SamplesPerFrame;
            int bytesToCopy = framesToCopy * BytesPerFrame;
            int samplesToCopy = framesToCopy * SamplesPerFrame;
            Array.Copy(audio.AudioByteArray, 0, output, 0, bytesToCopy);

            //We're gonna be doing a lot of seeking, so make sure the seek table is built
            if (!audio.SelfCalculatedSeekTable)
            {
                CalculateSeekTable(audio, alignment);
            }

            int totalSamples = loopEnd + samplesToAdd;
            int samplesToEncode = totalSamples - samplesToCopy;

            short[] history = audio.GetPcmAudioLooped(samplesToCopy, 16, loopStart, loopEnd, true);
            short[] pcm = audio.GetPcmAudioLooped(samplesToCopy, samplesToEncode, loopStart, loopEnd);
            var adpcm = Encode.EncodeAdpcm(pcm, audio.Coefs, history[1], history[0], samplesToEncode);

            Array.Copy(adpcm, 0, output, bytesToCopy, adpcm.Length);

            audio.AudioByteArrayAligned = output;
            audio.LoopAlignment = alignment;
            audio.LoopStartAligned = loopStart;
            audio.LoopEndAligned = loopEnd;
            audio.NumSamplesAligned = totalSamples;
        }
    }
}
