using System;
using VGAudio.Codecs;
using VGAudio.Codecs.CriHca;
using VGAudio.Formats.Pcm16;
using VGAudio.Utilities;

namespace VGAudio.Formats.CriHca
{
    public class CriHcaFormat : AudioFormatBase<CriHcaFormat, CriHcaFormatBuilder, CriHcaParameters>
    {
        public HcaInfo Hca { get; }

        public byte[][] AudioData { get; }

        public CriHcaFormat() { }

        internal CriHcaFormat(CriHcaFormatBuilder b) : base(b)
        {
            AudioData = b.AudioData;
            Hca = b.Hca;
        }

        public override Pcm16Format ToPcm16() => ToPcm16(null);
        public override Pcm16Format ToPcm16(CodecParameters config) => ToPcm16(new CriHcaParameters(config));
        public override Pcm16Format ToPcm16(CriHcaParameters config)
        {
            var audio = CriHcaDecoder.Decode(Hca, AudioData, config);
            return new Pcm16FormatBuilder(audio, SampleRate)
                .WithLoop(Looping, UnalignedLoopStart, UnalignedLoopEnd)
                .Build();
        }

        public override CriHcaFormat EncodeFromPcm16(Pcm16Format pcm16, CriHcaParameters config)
        {
            config.ChannelCount = pcm16.ChannelCount;
            config.SampleRate = pcm16.SampleRate;
            config.SampleCount = pcm16.SampleCount;
            var progress = config.Progress;

            var encoder = CriHcaEncoder.InitializeNew(config);
            const int frameSamples = 1024;
            var pcm = pcm16.Channels;
            var pcmBuffer = encoder.PcmBuffer;
            var hcaBuffer = encoder.HcaBuffer;

            progress?.SetTotal(encoder.Hca.FrameCount);

            var audio = Helpers.CreateJaggedArray<byte[][]>(encoder.Hca.FrameCount, encoder.Hca.FrameSize);

            for (int i = 0; i < encoder.Hca.FrameCount - 10; i++)
            {
                for (int c = 0; c < encoder.Hca.ChannelCount; c++)
                {
                    Array.Copy(pcm[c], frameSamples * i, pcmBuffer[c], 0, frameSamples);
                }

                encoder.EncodeFrame();
                Array.Copy(hcaBuffer, audio[i], hcaBuffer.Length);
                progress?.ReportAdd(1);
            }
            var builder = new CriHcaFormatBuilder(audio, encoder.Hca);
            return builder.Build();
        }

        public override CriHcaFormat EncodeFromPcm16(Pcm16Format pcm16)
        {
            var config = new CriHcaParameters
            {
                ChannelCount = pcm16.ChannelCount,
                SampleRate = pcm16.SampleRate
            };

            return EncodeFromPcm16(pcm16, config);
        }

        protected override CriHcaFormat GetChannelsInternal(int[] channelRange)
        {
            throw new NotImplementedException();
        }

        protected override CriHcaFormat AddInternal(CriHcaFormat format)
        {
            throw new NotImplementedException();
        }

        public override CriHcaFormatBuilder GetCloneBuilder()
        {
            throw new NotImplementedException();
        }
    }
}
