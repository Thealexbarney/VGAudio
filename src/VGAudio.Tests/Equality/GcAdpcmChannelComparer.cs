using System.Collections.Generic;
using System.Linq;
using VGAudio.Formats.GcAdpcm;

namespace VGAudio.Tests.Equality
{
    public class GcAdpcmChannelComparer : EqualityComparer<GcAdpcmChannel>
    {
        public override bool Equals(GcAdpcmChannel x, GcAdpcmChannel y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return
                x.SampleCount == y.SampleCount &&
                x.Gain == y.Gain &&
                x.StartContext.Hist1 == y.StartContext.Hist1 &&
                x.StartContext.Hist2 == y.StartContext.Hist2 &&
                x.Coefs.SequenceEqual(y.Coefs) &&
                x.Adpcm.SequenceEqual(y.Adpcm);
        }

        public override int GetHashCode(GcAdpcmChannel obj)
        {
            unchecked
            {
                if (obj == null) return 0;
                int hashCode = obj.SampleCount;
                hashCode = (hashCode * 397) ^ obj.Gain;
                hashCode = (hashCode * 397) ^ obj.StartContext.Hist1;
                hashCode = (hashCode * 397) ^ obj.StartContext.Hist2;
                return hashCode;
            }
        }
    }
}
