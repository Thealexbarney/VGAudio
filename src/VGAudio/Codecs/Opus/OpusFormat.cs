using System;
using System.Collections.Generic;
using System.Linq;
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
        public List<NxOpusFrame> Frames { get; }

        public OpusFormat() { }

        internal OpusFormat(OpusFormatBuilder b) : base(b)
        {
            Frames = b.Frames;
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

            for (int i = 0; i < Frames.Count; i++)
            {
                dec.Decode(Frames[i].Data, 0, Frames[i].Data.Length, pcmBuffer, 0, maxSampleCount);

                short[][] deinterleaved = pcmBuffer.DeInterleave(2, 2);

                for (int c = 0; c < ChannelCount; c++)
                {
                    Array.Copy(deinterleaved[c], 0, pcmOut[c], outPos, Frames[i].SampleCount);
                }

                outPos += Frames[i].SampleCount;
            }

            return pcmOut;
        }

        public override OpusFormat EncodeFromPcm16(Pcm16Format pcm16)
        {
            throw new NotImplementedException();
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
