using System;
using VGAudio.Codecs.CriHca;

namespace VGAudio.Formats.CriHca
{
    public class CriHcaFormatBuilder : AudioFormatBaseBuilder<CriHcaFormat, CriHcaFormatBuilder, CriHcaParameters>
    {
        public byte[][] AudioData { get; }
        public override int ChannelCount => Hca?.ChannelCount ?? 0;
        public HcaInfo Hca { get; }

        public CriHcaFormatBuilder(byte[][] audioData, HcaInfo hca)
        {
            AudioData = audioData;
            Hca = hca;
            SampleRate = hca.SampleRate;
            SampleCount = hca.SampleCount;

            if (hca.Looping)
            {
                Looping = true;
                LoopStart = hca.LoopStartSample;
                LoopEnd = hca.LoopEndSample;
            }
        }

        public override CriHcaFormatBuilder WithLoop(bool loop, int loopStart, int loopEnd)
        {
            base.WithLoop(loop, loopStart, loopEnd);

            if (loop && (loopStart != Hca.LoopStartSample || loopEnd != Hca.LoopEndSample))
            {
                throw new NotSupportedException("Changing the loop points on HCA audio without re-encoding is not supported.");
            }

            WithLoopImpl(loop);
            return this;
        }

        public override CriHcaFormatBuilder WithLoop(bool loop)
        {
            base.WithLoop(loop);

            WithLoopImpl(loop);
            return this;
        }

        private void WithLoopImpl(bool loop)
        {
            if (loop && !Hca.Looping)
            {
                throw new NotSupportedException("Adding a loop to HCA audio without re-encoding is not supported.");
            }

            Hca.Looping = loop;
        }

        public override CriHcaFormat Build() => new CriHcaFormat(this);
    }
}
