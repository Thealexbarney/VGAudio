namespace DspAdpcm.Formats.Adpcm
{
    /// <summary>
    /// Defines an audio track in an ADPCM audio
    /// stream. Each track is composed of one
    /// or two channels.
    /// </summary>
    public class AdpcmTrack
    {
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

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            var item = obj as AdpcmTrack;

            if (item == null)
            {
                return false;
            }

            return
                item.Volume == Volume &&
                item.Panning == Panning &&
                item.ChannelCount == ChannelCount &&
                item.ChannelLeft == ChannelLeft &&
                item.ChannelRight == ChannelRight;
        }

        /// <summary>
        /// Returns a hash code for the <see cref="AdpcmTrack"/> instance.
        /// </summary>
        /// <returns>A hash code for the <see cref="AdpcmTrack"/> instance.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Volume.GetHashCode();
                hashCode = (hashCode * 397) ^ Panning.GetHashCode();
                hashCode = (hashCode * 397) ^ ChannelCount.GetHashCode();
                hashCode = (hashCode * 397) ^ ChannelLeft.GetHashCode();
                hashCode = (hashCode * 397) ^ ChannelRight.GetHashCode();
                return hashCode;
            }
        }

        internal AdpcmTrack Clone() => (AdpcmTrack)MemberwiseClone();
    }
}
