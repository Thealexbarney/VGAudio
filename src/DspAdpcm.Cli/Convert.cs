using System;
using System.Collections.Generic;
using System.IO;
using DspAdpcm.Formats;

#if NET20
using DspAdpcm.Compatibility.LinqBridge;
#else
using System.Linq;
#endif

namespace DspAdpcm.Cli
{
    internal class Convert
    {
        private Convert() { }
        private AudioData Audio { get; set; }
        private object Configuration { get; set; }

        public static bool ConvertFile(Options options)
        {
            if (options.Job != JobType.Convert) return false;

            var converter = new Convert();

            foreach (AudioFile file in options.InFiles)
            {
                converter.ReadFile(file);
            }

            if (!options.KeepConfiguration)
            {
                converter.Configuration = null;
            }

            converter.EncodeFiles(options);
            converter.WriteFile(options.OutFiles[0].Path, options.OutFiles[0].Type);

            return true;
        }

        private void ReadFile(AudioFile file)
        {
            using (var stream = new FileStream(file.Path, FileMode.Open))
            {
                ContainerType type;
                ContainerTypes.Containers.TryGetValue(file.Type, out type);

                if (type == null)
                {
                    throw new ArgumentOutOfRangeException(nameof(file.Type), file.Type, null);
                }

                file.Audio = type.Read(stream);
            }
        }

        private void WriteFile(string fileName, FileType fileType)
        {
            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                ContainerType type;
                ContainerTypes.Containers.TryGetValue(fileType, out type);

                if (type == null)
                {
                    throw new ArgumentOutOfRangeException(nameof(fileType), fileType, null);
                }

                type.Write(Audio, stream);
            }
        }

        private void EncodeFiles(Options options)
        {
            foreach (AudioFile file in options.InFiles.Where(x => x.Channels != null))
            {
                IAudioFormat format = file.Audio.GetAllFormats().First();
                file.Audio = new AudioData(format.GetChannels(file.Channels));
            }

            Audio = options.InFiles[0].Audio;

            List<AudioData> toMerge = options.InFiles.Skip(1).Select(x => x.Audio).ToList();

            Audio.Add(toMerge);

            if (options.NoLoop)
            {
                Audio.SetLoop(false);
            }

            if (options.Loop)
            {
                Audio.SetLoop(options.LoopStart, options.LoopEnd);
            }
        }
    }
}
