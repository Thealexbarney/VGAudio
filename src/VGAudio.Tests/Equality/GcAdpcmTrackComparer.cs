using System.Collections.Generic;
using VGAudio.Formats.GcAdpcm;

namespace VGAudio.Tests.Equality
{
    public sealed class GcAdpcmTrackComparer : EqualityComparer<GcAdpcmTrack>
    {
        public override bool Equals(GcAdpcmTrack x, GcAdpcmTrack y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return
                x.ChannelCount == y.ChannelCount &&
                x.ChannelLeft == y.ChannelLeft &&
                x.ChannelRight == y.ChannelRight &&
                x.Panning == y.Panning &&
                x.Volume == y.Volume;
        }

        public override int GetHashCode(GcAdpcmTrack obj)
        {
            unchecked
            {
                if (obj == null) return 0;
                int hashCode = obj.ChannelCount;
                hashCode = (hashCode * 397) ^ obj.ChannelLeft;
                hashCode = (hashCode * 397) ^ obj.ChannelRight;
                hashCode = (hashCode * 397) ^ obj.Panning;
                hashCode = (hashCode * 397) ^ obj.Volume;
                return hashCode;
            }
        }
    }
}
