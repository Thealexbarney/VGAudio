using System;
using System.Collections.Generic;
using System.Linq;
using Concentus.Enums;
using Concentus.Structs;
using VGAudio.Containers.Opus;
using VGAudio.Formats;
using VGAudio.Formats.Pcm16;
using VGAudio.Utilities;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Codecs.Opus
{
    public class OpusFormat : AudioFormatBase<OpusFormat, OpusFormatBuilder, OpusParameters>
    {
        public int EncoderDelay { get; }

        public List<NxOpusFrame> Frames { get; }

        public OpusFormat() { }

        internal OpusFormat(OpusFormatBuilder b) : base(b)
        {
            Frames = b.Frames;
            EncoderDelay = b.EncoderDelay;
        }

        public override Pcm16Format ToPcm16()
        {
            short[][] audio = Decode();

            return new Pcm16FormatBuilder(audio, SampleRate)
                .WithLoop(Looping, UnalignedLoopStart, UnalignedLoopEnd)
                .Build();
        }

        private short[][] Decode()
        {
            var dec = new OpusDecoder(48000, ChannelCount);

            int maxSampleCount = Frames.Max(x => x.SampleCount);

            var pcmOut = CreateJaggedArray<short[][]>(ChannelCount, SampleCount);
            var pcmBuffer = new short[ChannelCount * maxSampleCount];
            int outPos = 0;
            int remaining = SampleCount;

            for (int i = 0; i < Frames.Count; i++)
            {
                int frameSamples = Math.Min(remaining, Frames[i].SampleCount);
                dec.Decode(Frames[i].Data, 0, Frames[i].Data.Length, pcmBuffer, 0, maxSampleCount);

                short[][] deinterleaved = pcmBuffer.DeInterleave(1, 2);

                CopyBuffer(deinterleaved, frameSamples, pcmOut, EncoderDelay, outPos);

                outPos += frameSamples;
                remaining -= frameSamples;
            }

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
            int srcStart = Clamp(0 - currentIndex, 0, bufferLength);
            int destStart = Math.Max(currentIndex, 0);

            int length = Math.Min(bufferLength - srcStart, remainingElements);
            if (length <= 0) return;

            for (int c = 0; c < bufferOut.Length; c++)
            {
                Array.Copy(bufferIn[c], srcStart, bufferOut[c], destStart, length);
            }
        }

        public override OpusFormat EncodeFromPcm16(Pcm16Format pcm16)
        {
            const int frameSize = 960;
            var encoder = new OpusEncoder(pcm16.SampleRate, pcm16.ChannelCount, OpusApplication.OPUS_APPLICATION_AUDIO);

            int inPos = 0;
            int remaining = pcm16.SampleCount;

            short[] pcmData = pcm16.Channels.Interleave(1);

            // Encoded data shouldn't be larger than the input pcm
            var buffer = new byte[frameSize * pcm16.ChannelCount];
            var frames = new List<NxOpusFrame>();

            short[] encodeInput = pcmData;

            while (remaining > 0)
            {
                int encodeCount = Math.Min(frameSize, remaining);

                if (remaining < frameSize * pcm16.ChannelCount)
                {
                    encodeInput = new short[frameSize * pcm16.ChannelCount];
                    Array.Copy(pcmData, inPos, encodeInput, 0, encodeCount);
                    inPos = 0;
                }

                int frameLength = encoder.Encode(encodeInput, inPos, frameSize, buffer, 0, buffer.Length);

                var frame = new NxOpusFrame();
                frame.Length = frameLength;
                frame.Data = new byte[frameLength];
                frame.FinalRange = encoder.FinalRange;

                Array.Copy(buffer, frame.Data, frameLength);

                frames.Add(frame);

                remaining -= encodeCount;
                inPos += encodeCount * pcm16.ChannelCount;
            }

            OpusFormat format = new OpusFormatBuilder(pcm16.ChannelCount, pcm16.SampleCount, frames).Build();

            return format;
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
