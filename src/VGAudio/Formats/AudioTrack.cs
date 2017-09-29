using System;
using System.Collections.Generic;
using VGAudio.Utilities;

namespace VGAudio.Formats
{
    /// <summary>
    /// Defines an audio track in an audio stream.
    /// Each track is composed of one or two channels.
    /// </summary>
    public class AudioTrack
    {
        public AudioTrack(int channelCount, int channelLeft, int channelRight, int panning, int volume)
        {
            ChannelCount = channelCount;
            ChannelLeft = channelLeft;
            ChannelRight = channelRight;
            Panning = panning;
            Volume = volume;
        }

        public AudioTrack(int channelCount, int channelLeft, int channelRight)
        {
            ChannelCount = channelCount;
            ChannelLeft = channelLeft;
            ChannelRight = channelRight;
        }

        public AudioTrack() { }

        /// <summary>
        /// The volume of the track. Ranges from
        /// 0 to 127 (0x7f).
        /// </summary>
        public int Volume { get; set; } = 0x7f;

        /// <summary>
        /// The panning of the track. Ranges from
        /// 0 (Completely to the left) to 127 (0x7f)
        /// (Completely to the right) with the center
        /// at 64 (0x40).
        /// </summary>
        public int Panning { get; set; } = 0x40;

        /// <summary>
        /// The number of channels in the track.
        /// If <c>1</c>, only <see cref="ChannelLeft"/>
        /// will be used for the mono track.
        /// If <c>2</c>, both <see cref="ChannelLeft"/>
        /// and <see cref="ChannelRight"/> will be used.
        /// </summary>
        public int ChannelCount { get; set; }

        /// <summary>
        /// The zero-based ID of the left channel in a stereo
        /// track, or the only channel in a mono track.
        /// </summary>
        public int ChannelLeft { get; set; }

        /// <summary>
        /// The zero-based ID of the right channel in
        /// a stereo track.
        /// </summary>
        public int ChannelRight { get; set; }

        public int SurroundPanning { get; set; }
        public int Flags { get; set; }

        public static IEnumerable<AudioTrack> GetDefaultTrackList(int channelCount)
        {
            int trackCount = channelCount.DivideByRoundUp(2);
            for (int i = 0; i < trackCount; i++)
            {
                int trackChannelCount = Math.Min(channelCount - i * 2, 2);
                yield return new AudioTrack
                {
                    ChannelCount = trackChannelCount,
                    ChannelLeft = i * 2,
                    ChannelRight = trackChannelCount >= 2 ? i * 2 + 1 : 0
                };
            }
        }
    }
}
