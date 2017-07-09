using VGAudio.Codecs.CriHca;

namespace VGAudio.Formats.CriHca
{
    public class CriHcaFormatBuilder : AudioFormatBaseBuilder<CriHcaFormat, CriHcaFormatBuilder, CriHcaParameters>
    {
        public byte[][] AudioData { get; }
        protected internal override int ChannelCount => Hca?.ChannelCount ?? 0;
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
                LoopStart = hca.LoopStartFrame * 1024 + hca.PreLoopSamples - hca.InsertedSamples;
                LoopEnd = (hca.LoopEndFrame + 1) * 1024 - hca.PostLoopSamples - hca.InsertedSamples;
            }
        }

        public override CriHcaFormat Build() => new CriHcaFormat(this);
    }
}
