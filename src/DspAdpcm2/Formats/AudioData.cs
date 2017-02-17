using System;
using System.Collections.Generic;
using System.Linq;

namespace DspAdpcm.Formats
{
    public class AudioData
    {
        private Dictionary<Type, IAudioFormat> Formats { get; } = new Dictionary<Type, IAudioFormat>();

        private void AddFormat(IAudioFormat format) => Formats.Add(format.GetType(), format);

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

        public void SetLoop(int loopStart, int loopEnd)
        {
            foreach (IAudioFormat format in Formats.Values)
            {
                format.SetLoop(loopStart, loopEnd);
            }
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