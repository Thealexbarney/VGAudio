using System.Linq;
using System.Collections.Generic;
using VGAudio.Formats;

namespace VGAudio.Tests.Equality
{
    public class Pcm8FormatComparer : EqualityComparer<Pcm8Format>
    {
        public override bool Equals(Pcm8Format x, Pcm8Format y)
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
                (x.Tracks ?? new List<AudioTrack>()).SequenceEqual(y.Tracks ?? new List<AudioTrack>(), new AudioTrackComparer()) &&
                !x.Channels.Where((t, i) => !t.SequenceEqual(y.Channels[i])).Any();
        }

        public override int GetHashCode(Pcm8Format obj)
        {
            unchecked
            {
                if (obj == null) return 0;
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
