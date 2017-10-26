using static VGAudio.Codecs.CriHca.ChannelType;
using static VGAudio.Codecs.CriHca.CriHcaConstants;

namespace VGAudio.Codecs.CriHca
{
    public class CriHcaFrame
    {
        public HcaInfo Hca { get; }
        public CriHcaChannel[] Channels { get; }
        public byte[] AthCurve { get; }
        public int AcceptableNoiseLevel { get; set; }
        public int EvaluationBoundary { get; set; }

        public CriHcaFrame(HcaInfo hca)
        {
            Hca = hca;
            ChannelType[] channelTypes = GetChannelTypes(hca);
            Channels = new CriHcaChannel[hca.ChannelCount];

            for (int i = 0; i < Channels.Length; i++)
            {
                Channels[i] = new CriHcaChannel
                {
                    Type = channelTypes[i],
                    CodedScaleFactorCount = channelTypes[i] == StereoSecondary
                        ? hca.BaseBandCount
                        : hca.BaseBandCount + hca.StereoBandCount
                };
            }

            AthCurve = hca.UseAthCurve ? ScaleAthCurve(hca.SampleRate) : new byte[SamplesPerSubFrame];
        }

        private static ChannelType[] GetChannelTypes(HcaInfo hca)
        {
            int channelsPerTrack = hca.ChannelCount / hca.TrackCount;
            if (hca.StereoBandCount == 0 || channelsPerTrack == 1) { return new ChannelType[8]; }

            switch (channelsPerTrack)
            {
                case 2: return new[] { StereoPrimary, StereoSecondary };
                case 3: return new[] { StereoPrimary, StereoSecondary, Discrete };
                case 4 when hca.ChannelConfig != 0: return new[] { StereoPrimary, StereoSecondary, Discrete, Discrete };
                case 4 when hca.ChannelConfig == 0: return new[] { StereoPrimary, StereoSecondary, StereoPrimary, StereoSecondary };
                case 5 when hca.ChannelConfig > 2: return new[] { StereoPrimary, StereoSecondary, Discrete, Discrete, Discrete };
                case 5 when hca.ChannelConfig <= 2: return new[] { StereoPrimary, StereoSecondary, Discrete, StereoPrimary, StereoSecondary };
                case 6: return new[] { StereoPrimary, StereoSecondary, Discrete, Discrete, StereoPrimary, StereoSecondary };
                case 7: return new[] { StereoPrimary, StereoSecondary, Discrete, Discrete, StereoPrimary, StereoSecondary, Discrete };
                case 8: return new[] { StereoPrimary, StereoSecondary, Discrete, Discrete, StereoPrimary, StereoSecondary, StereoPrimary, StereoSecondary };
                default: return new ChannelType[channelsPerTrack];
            }
        }

        /// <summary>
        /// Scales an ATH curve to the specified frequency.
        /// </summary>
        /// <param name="frequency">The frequency to scale the curve to.</param>
        /// <returns>The scaled ATH curve</returns>
        /// <remarks>The original ATH curve is for a frequency of 41856 Hz.</remarks>
        private static byte[] ScaleAthCurve(int frequency)
        {
            var ath = new byte[SamplesPerSubFrame];

            int acc = 0;
            int i;
            for (i = 0; i < ath.Length; i++)
            {
                acc += frequency;
                int index = acc >> 13;

                if (index >= CriHcaTables.AthCurve.Length)
                {
                    break;
                }
                ath[i] = CriHcaTables.AthCurve[index];
            }

            for (; i < ath.Length; i++)
            {
                ath[i] = 0xff;
            }

            return ath;
        }
    }
}
