using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VGAudio.Formats;

namespace VGAudio.Tests.Equality
{
    public class GcAdpcmFormatComparer : EqualityComparer<GcAdpcmFormat>
    {
        public override bool Equals(GcAdpcmFormat x, GcAdpcmFormat y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return
                x.SampleCount == y.SampleCount &&
                x.ChannelCount == y.ChannelCount &&
                x.LoopStart == y.LoopStart &&
                x.LoopEnd == y.LoopEnd &&
                x.Looping == y.Looping &&
                x.Tracks.SequenceEqual(y.Tracks, new GcAdpcmTrackComparer()) &&
                x.Channels.SequenceEqual(y.Channels, new GcAdpcmChannelComparer());

        }

        public override int GetHashCode(GcAdpcmFormat obj)
        {
            unchecked
            {
                Debug.Assert(obj != null, "obj != null");
                int hashCode = obj.SampleCount;
                hashCode = (hashCode * 397) ^ obj.ChannelCount;
                hashCode = (hashCode * 397) ^ obj.LoopStart;
                hashCode = (hashCode * 397) ^ obj.LoopEnd;
                hashCode = (hashCode * 397) ^ obj.Looping.GetHashCode();
                return hashCode;
            }
        }
    }
}
