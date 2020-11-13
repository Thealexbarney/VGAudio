using System;
using System.Collections.Generic;
using System.Linq;
using Concentus.Enums;
using Concentus.Structs;
using VGAudio.Codecs;
using VGAudio.Codecs.Opus;
using VGAudio.Formats.Pcm16;
using VGAudio.Utilities;

namespace VGAudio.Formats.Opus
{
    public class OpusFormat : AudioFormatBase<OpusFormat, OpusFormatBuilder, OpusParameters>
    {
        public int PreSkipCount { get; }

        public List<OpusFrame> Frames { get; }

        public bool HasFinalRange { get; private set; }

        public OpusFormat() { }

        internal OpusFormat(OpusFormatBuilder b) : base(b)
        {
            Frames = b.Frames;
            PreSkipCount = b.PreSkip;
            HasFinalRange = b.HasFinalRangeSet;
        }

        public override Pcm16Format ToPcm16() => ToPcm16(null);
        public override Pcm16Format ToPcm16(CodecParameters config) => ToPcm16(new OpusParameters(config));
        public override Pcm16Format ToPcm16(OpusParameters config)
        {
            short[][] audio = Decode(config);

            return new Pcm16FormatBuilder(audio, SampleRate)
                .WithLoop(Looping, UnalignedLoopStart, UnalignedLoopEnd)
                .Build();
        }

        private short[][] Decode(OpusParameters config)
        {
            var dec = new OpusDecoder(SampleRate, ChannelCount);

            int maxSampleCount = Frames.Max(x => x.SampleCount);

            var pcmOut = Helpers.CreateJaggedArray<short[][]>(ChannelCount, SampleCount);
            var pcmBuffer = new short[ChannelCount * maxSampleCount];
            int outPos = 0;
            int remaining = SampleCount + PreSkipCount;

            config.Progress?.SetTotal(Frames.Count);

            for (int i = 0; i < Frames.Count; i++)
            {
                int frameSamples = Math.Min(remaining, Frames[i].SampleCount);
                dec.Decode(Frames[i].Data, 0, Frames[i].Data.Length, pcmBuffer, 0, maxSampleCount);

                short[][] deinterleaved = pcmBuffer.DeInterleave(1, ChannelCount);

                CopyBuffer(deinterleaved, frameSamples, pcmOut, PreSkipCount, outPos);

                outPos += frameSamples;
                remaining -= frameSamples;

                config.Progress?.ReportAdd(1);
            }

            config.Progress?.SetTotal(0);

            return pcmOut;
        }

        private static void CopyBuffer(short[][] bufferIn, int inputLength, short[][] bufferOut, int startIndex, int currentPosition)
        {
            if (bufferIn == null || bufferOut == null || bufferIn.Length == 0 || bufferOut.Length == 0)
            {
                throw new ArgumentException(
                    $"{nameof(bufferIn)} and {nameof(bufferOut)} must be non-null with a length greater than 0");
            }

            int bufferLength = inputLength;
            int outLength = bufferOut[0].Length;

            int currentIndex = currentPosition - startIndex;
            int remainingElements = Math.Min(outLength - currentIndex, outLength);
            int srcStart = Helpers.Clamp(0 - currentIndex, 0, bufferLength);
            int destStart = Math.Max(currentIndex, 0);

            int length = Math.Min(bufferLength - srcStart, remainingElements);
            if (length <= 0) return;

            for (int c = 0; c < bufferOut.Length; c++)
            {
                Array.Copy(bufferIn[c], srcStart, bufferOut[c], destStart, length);
            }
        }

        public override OpusFormat EncodeFromPcm16(Pcm16Format pcm16) => EncodeFromPcm16(pcm16, new OpusParameters());

        public override OpusFormat EncodeFromPcm16(Pcm16Format pcm16, OpusParameters config)
        {
            const int frameSize = 960;

            var encoder = new OpusEncoder(pcm16.SampleRate, pcm16.ChannelCount, OpusApplication.OPUS_APPLICATION_AUDIO);
            encoder.UseVBR = !config.EncodeCbr;

            if (config.Bitrate > 0) encoder.Bitrate = config.Bitrate;

            int inPos = 0;
            int remaining = pcm16.SampleCount;

            short[] pcmData = pcm16.Channels.Interleave(1);

            // Encoded data shouldn't be larger than the input pcm
            var buffer = new byte[frameSize * pcm16.ChannelCount];
            var frames = new List<OpusFrame>();

            short[] encodeInput = pcmData;

            config.Progress?.SetTotal(pcm16.SampleCount.DivideByRoundUp(frameSize));

            while (remaining >= 0)
            {
                int encodeCount = Math.Min(frameSize, remaining);

                if (remaining < frameSize)
                {
                    encodeInput = new short[frameSize * pcm16.ChannelCount];
                    Array.Copy(pcmData, inPos, encodeInput, 0, encodeCount * pcm16.ChannelCount);
                    inPos = 0;
                }

                int frameLength = encoder.Encode(encodeInput, inPos, frameSize, buffer, 0, buffer.Length);

                var frame = new OpusFrame();
                frame.Length = frameLength;
                frame.Data = new byte[frameLength];
                frame.FinalRange = encoder.FinalRange;
                frame.SampleCount = frameSize;

                Array.Copy(buffer, frame.Data, frameLength);

                frames.Add(frame);

                if (remaining == 0) break;

                remaining -= encodeCount;
                inPos += encodeCount * pcm16.ChannelCount;

                config.Progress?.ReportAdd(1);
            }

            OpusFormat format = new OpusFormatBuilder(pcm16.SampleCount, pcm16.SampleRate, pcm16.ChannelCount, encoder.Lookahead, frames)
                .WithLoop(pcm16.Looping, pcm16.LoopStart, pcm16.LoopEnd)
                .Build();

            config.Progress?.SetTotal(0);

            return format;
        }

        public void EnsureHasFinalRange()
        {
            if (HasFinalRange) return;

            int maxSampleCount = Frames.Max(x => x.SampleCount);
            var dec = new OpusDecoder(SampleRate, ChannelCount);
            var pcm = new short[5760 * ChannelCount];

            foreach (OpusFrame frame in Frames)
            {
                dec.Decode(frame.Data, 0, frame.Data.Length, pcm, 0, maxSampleCount);
                frame.FinalRange = dec.FinalRange;
            }

            HasFinalRange = true;
        }

        protected override OpusFormat GetChannelsInternal(int[] channelRange)
        {
            throw new NotImplementedException();
        }

        protected override OpusFormat AddInternal(OpusFormat format)
        {
            throw new NotImplementedException();
        }

        public override OpusFormatBuilder GetCloneBuilder()
        {
            throw new NotImplementedException();
        }
    }
}
