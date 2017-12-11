using System;
using VGAudio.Codecs;
using VGAudio.Codecs.Atrac9;
using VGAudio.Formats.Pcm16;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Formats.Atrac9
{
    public class Atrac9Format : AudioFormatBase<Atrac9Format, Atrac9FormatBuilder, Atrac9Parameters>
    {
        public byte[][] AudioData { get; }
        public Atrac9Config Config { get; }
        public int EncoderDelay { get; }

        internal Atrac9Format(Atrac9FormatBuilder b) : base(b)
        {
            AudioData = b.AudioData;
            Config = b.Config;
            EncoderDelay = b.EncoderDelay;
        }

        public override Pcm16Format ToPcm16() => ToPcm16(null);
        public override Pcm16Format ToPcm16(CodecParameters config)
        {
            short[][] audio = Decode(config);

            return new Pcm16FormatBuilder(audio, SampleRate)
                .WithLoop(Looping, UnalignedLoopStart, UnalignedLoopEnd)
                .Build();
        }

        private short[][] Decode(CodecParameters parameters)
        {
            IProgressReport progress = parameters?.Progress;
            progress?.SetTotal(AudioData.Length);

            var decoder = new Atrac9Decoder();
            decoder.Initialize(Config.ConfigData);
            Atrac9Config config = decoder.Config;
            var pcmOut = CreateJaggedArray<short[][]>(config.ChannelCount, SampleCount);
            var pcmBuffer = CreateJaggedArray<short[][]>(config.ChannelCount, config.SuperframeSamples);

            for (int i = 0; i < AudioData.Length; i++)
            {
                decoder.Decode(AudioData[i], pcmBuffer);
                CopyBuffer(pcmBuffer, pcmOut, EncoderDelay, i);
                progress?.ReportAdd(1);
            }
            return pcmOut;
        }

        private static void CopyBuffer(short[][] bufferIn, short[][] bufferOut, int startIndex, int bufferIndex)
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
            int srcStart = Clamp(0 - currentIndex, 0, bufferLength);
            int destStart = Math.Max(currentIndex, 0);

            int length = Math.Min(bufferLength - srcStart, remainingElements);
            if (length <= 0) return;

            for (int c = 0; c < bufferOut.Length; c++)
            {
                Array.Copy(bufferIn[c], srcStart, bufferOut[c], destStart, length);
            }
        }

        public override Atrac9Format EncodeFromPcm16(Pcm16Format pcm16)
        {
            throw new NotImplementedException();
        }

        public override Atrac9FormatBuilder GetCloneBuilder()
        {
            throw new NotImplementedException();
        }

        protected override Atrac9Format AddInternal(Atrac9Format format)
        {
            throw new NotImplementedException();
        }

        protected override Atrac9Format GetChannelsInternal(int[] channelRange)
        {
            throw new NotImplementedException();
        }
    }
}
