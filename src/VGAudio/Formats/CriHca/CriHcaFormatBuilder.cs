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

        public override CriHcaFormat Build() => new CriHcaFormat(this);
    }
}
