using System;
using System.Collections.Generic;
using System.Linq;
using VGAudio.Codecs;
using VGAudio.Formats.Pcm16;

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

        public T GetFormat<T>(CodecParameters configuration = null) where T : class, IAudioFormat, new()
        {
            var format = GetAudioFormat<T>();

            if (format != null)
            {
                return format;
            }

            CreatePcm16(configuration);
            CreateFormat<T>(configuration);

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

        public static AudioData Combine(params AudioData[] audio)
        {
            if (audio == null || audio.Length <= 0 || audio.Any(x => x == null))
                throw new ArgumentException("Audio cannot be null, empty, or have any null elements");

            List<Type> commonTypes = audio
                .Select(x => x.ListAvailableFormats())
                .Aggregate((x, y) => x.Intersect(y))
                .ToList();

            Type formatToUse;

            if (commonTypes.Count == 0 || commonTypes.Count == 1 && commonTypes.Contains(typeof(Pcm16Format)))
            {
                formatToUse = typeof(Pcm16Format);
                foreach (AudioData a in audio)
                {
                    a.CreatePcm16();
                }
            }
            else
            {
                formatToUse = commonTypes.First(x => x != typeof(Pcm16Format));
            }

            IAudioFormat combined = audio[0].Formats[formatToUse];

            foreach (IAudioFormat format in audio.Select(x => x.Formats[formatToUse]).Skip(1))
            {
                if (combined.TryAdd(format, out combined) == false)
                {
                    throw new ArgumentException("Audio streams cannot be added together");
                }
            }

            return new AudioData(combined);
        }

        private T GetAudioFormat<T>() where T : class, IAudioFormat
        {
            Formats.TryGetValue(typeof(T), out IAudioFormat format);

            return (T)format;
        }

        private void CreateFormat<T>(CodecParameters configuration = null) where T : class, IAudioFormat, new()
        {
            var pcm = GetAudioFormat<Pcm16Format>();
            AddFormat(new T().EncodeFromPcm16(pcm, configuration));
        }

        private void CreatePcm16(CodecParameters configuration = null)
        {
            if (GetAudioFormat<Pcm16Format>() == null)
            {
                AddFormat(Formats.First().Value.ToPcm16(configuration));
            }
        }
    }
}