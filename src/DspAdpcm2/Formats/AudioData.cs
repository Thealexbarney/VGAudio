using System.Collections.Generic;
using System.Linq;

namespace DspAdpcm.Formats
{
    public class AudioData
    {
        public List<IAudioFormat> Formats { get; set; } = new List<IAudioFormat>();

        public AudioData(IAudioFormat audioFormat)
        {
            Formats.Add(audioFormat);
        }

        public T GetFormat<T>() where T : class, IAudioFormat
        {
            T format = GetAudioFormat<T>();

            if (format != null)
            {
                return format;
            }

            CreatePcm16();

            format = GetAudioFormat<T>();

            if (format != null)
            {
                return format;
            }

            return null;
        }

        private T GetAudioFormat<T>() where T : class, IAudioFormat
        {
            return Formats.OfType<T>().FirstOrDefault();
        }

        private void CreateFormat<T>()
        {
            
        }

        private void CreatePcm16()
        {
            if (GetAudioFormat<Pcm16Format>() != null)
            {
                return;
            }

            Formats.Add(Formats.First().ToPcm16());
        }
    }
}