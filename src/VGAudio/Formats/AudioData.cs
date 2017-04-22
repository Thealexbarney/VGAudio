using System;
using System.Collections.Generic;
using System.Linq;

namespace VGAudio.Formats
{
    public class AudioData
    {
        private Dictionary<Type, IAudioFormat> Formats { get; } = new Dictionary<Type, IAudioFormat>();

        private void AddFormat(IAudioFormat format) => Formats[format.GetType()] = format;

        public AudioData(IAudioFormat audioFormat)
        {
            AddFormat(audioFormat);
        }

        public T GetFormat<T>() where T : class, IAudioFormat, new()
        {
            T format = GetAudioFormat<T>();

            if (format != null)
            {
                return format;
            }

            CreatePcm16();
            CreateFormat<T>();

            return GetAudioFormat<T>();
        }

        public IEnumerable<IAudioFormat> GetAllFormats() => Formats.Values;

        public IEnumerable<Type> ListAvailableFormats() => Formats.Keys;

        public void SetLoop(bool loop, int loopStart, int loopEnd)
        {
            foreach (Type format in Formats.Keys.ToList())
            {
                Formats[format] = Formats[format].WithLoop(loop, loopStart, loopEnd);
            }
        }
        public void SetLoop(bool loop)
        {
            foreach (Type format in Formats.Keys.ToList())
            {
                Formats[format] = Formats[format].WithLoop(loop);
            }
        }

        public void Add(IList<AudioData> audio)
        {
            if (audio == null || audio.Count <= 0) return;

            List<Type> commonTypes = audio
                .Select(x => x.ListAvailableFormats())
                .Aggregate((x, y) => x.Intersect(y))
                .ToList();

            Type formatToUse;

            if (commonTypes.Count == 0 || commonTypes.Count == 1 && commonTypes.Contains(typeof(Pcm16Format)))
            {
                formatToUse = typeof(Pcm16Format);
            }
            else
            {
                formatToUse = commonTypes.First(x => x != typeof(Pcm16Format));
            }

            IAudioFormat baseFormat = Formats[formatToUse];

            foreach (IAudioFormat format in audio.Select(x => x.Formats[formatToUse]))
            {
                if (baseFormat.TryAdd(format) == false)
                {
                    throw new ArgumentException("Audio streams cannot be added together");
                }
            }

            Formats.Clear();
            Formats[baseFormat.GetType()] = baseFormat;
        }

        private T GetAudioFormat<T>() where T : class, IAudioFormat
        {
            IAudioFormat format;

            Formats.TryGetValue(typeof(T), out format);

            return (T)format;
        }

        private void CreateFormat<T>() where T : class, IAudioFormat, new()
        {
            Pcm16Format pcm = GetAudioFormat<Pcm16Format>();
            AddFormat(new T().EncodeFromPcm16(pcm));
        }

        private void CreatePcm16()
        {
            if (GetAudioFormat<Pcm16Format>() == null)
            {
                AddFormat(Formats.First().Value.ToPcm16());
            }
        }
    }
}