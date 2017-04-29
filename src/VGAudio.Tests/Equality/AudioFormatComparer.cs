using System.Collections.Generic;
using VGAudio.Formats;

namespace VGAudio.Tests.Equality
{
    public class AudioFormatComparer : EqualityComparer<IAudioFormat>
    {
        public override bool Equals(IAudioFormat x, IAudioFormat y)
        {
            if (x.GetType() != y.GetType()) return false;

            if (x.GetType() == typeof(Pcm16Format))
            {
                return new Pcm16FormatComparer().Equals(x as Pcm16Format, y as Pcm16Format);
            }
            if (x.GetType() == typeof(GcAdpcmFormat))
            {
                return new GcAdpcmFormatComparer().Equals(x as GcAdpcmFormat, y as GcAdpcmFormat);
            }

            return false;
        }

        public override int GetHashCode(IAudioFormat obj)
        {
            if (obj == null) return 0;
            if (obj.GetType() == typeof(Pcm16Format))
            {
                return new Pcm16FormatComparer().GetHashCode(obj as Pcm16Format);
            }
            if (obj.GetType() == typeof(GcAdpcmFormat))
            {
                return new GcAdpcmFormatComparer().GetHashCode(obj as GcAdpcmFormat);
            }

            return 0;
        }
    }
}
