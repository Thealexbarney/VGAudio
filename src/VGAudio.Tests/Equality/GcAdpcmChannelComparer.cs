using System.Collections.Generic;
using System.Diagnostics;
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
                x.Hist1 == y.Hist1 &&
                x.Hist2 == y.Hist2 &&
                x.Coefs.SequenceEqual(y.Coefs) &&
                x.Adpcm.SequenceEqual(y.Adpcm);
        }

        public override int GetHashCode(GcAdpcmChannel obj)
        {
            unchecked
            {
                Debug.Assert(obj != null, "obj != null");
                int hashCode = obj.SampleCount;
                hashCode = (hashCode * 397) ^ obj.Gain;
                hashCode = (hashCode * 397) ^ obj.Hist1;
                hashCode = (hashCode * 397) ^ obj.Hist2;
                return hashCode;
            }
        }
    }
}
