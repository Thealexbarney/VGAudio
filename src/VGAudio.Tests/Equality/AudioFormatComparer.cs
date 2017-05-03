using System.Collections.Generic;
using VGAudio.Formats;

namespace VGAudio.Tests.Equality
{
    public class AudioFormatComparer : EqualityComparer<IAudioFormat>
    {
        public override bool Equals(IAudioFormat x, IAudioFormat y)
        {
            if (x.GetType() != y.GetType()) return false;

            switch (x)
            {
                case Pcm16Format f:
                    return new Pcm16FormatComparer().Equals(f, y as Pcm16Format);
                case Pcm8Format f:
                    return new Pcm8FormatComparer().Equals(f, y as Pcm8Format);
                case GcAdpcmFormat f:
                    return new GcAdpcmFormatComparer().Equals(f, y as GcAdpcmFormat);
                default:
                    return false;
            }
        }

        public override int GetHashCode(IAudioFormat obj)
        {
            switch (obj)
            {
                case Pcm16Format f:
                    return new Pcm16FormatComparer().GetHashCode(f);
                case Pcm8Format f:
                    return new Pcm8FormatComparer().GetHashCode(f);
                case GcAdpcmFormat f:
                    return new GcAdpcmFormatComparer().GetHashCode(f);
                default:
                    return 0;
            }
        }
    }
}
