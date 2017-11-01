using VGAudio.Utilities;

namespace VGAudio.Codecs.CriHca
{
    public class HcaInfo
    {
        public int ChannelCount { get; set; }
        public int SampleRate { get; set; }
        public int SampleCount { get; set; }
        public int FrameCount { get; set; }
        public int InsertedSamples { get; set; }
        public int AppendedSamples { get; set; }
        public int HeaderSize { get; set; }

        public int FrameSize { get; set; }
        public int MinResolution { get; set; }
        public int MaxResolution { get; set; }
        public int TrackCount { get; set; }
        public int ChannelConfig { get; set; }
        public int TotalBandCount { get; set; }
        public int BaseBandCount { get; set; }
        public int StereoBandCount { get; set; }
        public int HfrBandCount { get; set; }
        public int BandsPerHfrGroup { get; set; }
        public int HfrGroupCount { get; set; }

        public int DecStereoType { get; set; }

        public bool Looping { get; set; }
        public int LoopStartFrame { get; set; }
        public int LoopEndFrame { get; set; }
        public int PreLoopSamples { get; set; }
        public int PostLoopSamples { get; set; }

        public int LoopStartSample => LoopStartFrame * 1024 + PreLoopSamples - InsertedSamples;
        public int LoopEndSample => (LoopEndFrame + 1) * 1024 - PostLoopSamples - InsertedSamples;

        public bool UseAthCurve { get; set; }

        public int VbrMaxFrameSize { get; set; }
        public int VbrNoiseLevel { get; set; }

        public int EncryptionType { get; set; }

        public float Volume { get; set; } = 1;

        public string Comment { get; set; }
        public int CommentLength => Comment?.Length ?? 0;

        public void CalculateHfrValues()
        {
            if (BandsPerHfrGroup <= 0) return;

            HfrBandCount = TotalBandCount - BaseBandCount - StereoBandCount;
            HfrGroupCount = HfrBandCount.DivideByRoundUp(BandsPerHfrGroup);
        }
    }
}
