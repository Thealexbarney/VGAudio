using System;
using System.IO;
using System.Linq;
using VGAudio.Containers;
using VGAudio.Formats;

namespace VGAudio.Cli
{
    internal class Convert
    {
        private Convert() { }
        private AudioData Audio { get; set; }
        private Configuration Configuration { get; set; }
        private ContainerType OutType { get; set; }

        public static bool ConvertFile(Options options)
        {
            if (options.Job != JobType.Convert) return false;

            var converter = new Convert();

            foreach (AudioFile file in options.InFiles)
            {
                converter.ReadFile(file);
            }

            converter.EncodeFiles(options);
            converter.SetConfiguration(options);
            converter.WriteFile(options.OutFiles[0].Path);

            return true;
        }

        private void ReadFile(AudioFile file)
        {
            using (var stream = new FileStream(file.Path, FileMode.Open))
            {
                ContainerTypes.Containers.TryGetValue(file.Type, out ContainerType type);

                if (type == null)
                {
                    throw new ArgumentOutOfRangeException(nameof(file.Type), file.Type, null);
                }

                AudioWithConfig audio = type.GetReader().ReadWithConfig(stream);
                file.Audio = audio.Audio;
                Configuration = audio.Configuration;
            }
        }

        private void WriteFile(string fileName)
        {
            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                OutType.GetWriter().WriteToStream(Audio, stream, Configuration);
            }
        }

        private void EncodeFiles(Options options)
        {
            foreach (AudioFile file in options.InFiles.Where(x => x.Channels != null))
            {
                IAudioFormat format = file.Audio.GetAllFormats().First();
                file.Audio = new AudioData(format.GetChannels(file.Channels.ToArray()));
            }

            AudioData[] toMerge = options.InFiles.Select(x => x.Audio).ToArray();

            Audio = AudioData.Combine(toMerge);

            if (options.NoLoop)
            {
                Audio.SetLoop(false);
            }

            if (options.Loop)
            {
                Audio.SetLoop(options.Loop, options.LoopStart, options.LoopEnd);
            }
        }

        private void SetConfiguration(Options options)
        {
            if (!options.KeepConfiguration)
            {
                Configuration = null;
            }

            if (!ContainerTypes.Containers.TryGetValue(options.OutFiles[0].Type, out ContainerType type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), "Output type not in type dictionary");
            }
            OutType = type;

            Configuration = OutType.GetConfiguration(options, Configuration);
        }
    }
}
