using System;
using VGAudio.Codecs;
using VGAudio.Codecs.CriHca;
using VGAudio.Formats.Pcm16;

namespace VGAudio.Formats.CriHca
{
    public class CriHcaFormat : AudioFormatBase<CriHcaFormat, CriHcaFormatBuilder, CriHcaParameters>
    {
        public HcaInfo Hca { get; }

        public byte[][] AudioData { get; }

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

        public override CriHcaFormat EncodeFromPcm16(Pcm16Format pcm16)
        {
            throw new NotImplementedException();
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
