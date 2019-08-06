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
        private bool ShowProgress { get; set; }

        public static bool ConvertFile(Options options) => ConvertFile(options, options.Files);

        public static bool ConvertFile(Options options, JobFiles files, bool showProgress = true)
        {
            if (options.Job != JobType.Convert && options.Job != JobType.Batch) return false;

            var converter = new Convert { ShowProgress = showProgress };

            foreach (AudioFile file in files.InFiles)
            {
                converter.ReadFile(file);
            }

            converter.EncodeFiles(files, options);
            converter.SetConfiguration(files.OutFiles[0].Type, options);
            converter.WriteFile(files.OutFiles[0].Path);

            return true;
        }

        private void ReadFile(AudioFile file)
        {
            using (var stream = new FileStream(file.Path, FileMode.Open, FileAccess.Read))
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
            string directory = Path.GetDirectoryName(fileName);
            if (!String.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite))
            using (var progress = new ProgressBar())
            {
                if (ShowProgress) Configuration.Progress = progress;
                OutType.GetWriter().WriteToStream(Audio, stream, Configuration);
            }
        }

        private void EncodeFiles(JobFiles files, Options options)
        {
            foreach (AudioFile file in files.InFiles.Where(x => x.Channels != null))
            {
                IAudioFormat format = file.Audio.GetAllFormats().First();
                file.Audio = new AudioData(format.GetChannels(file.Channels.ToArray()));
            }

            AudioData[] toMerge = files.InFiles.Select(x => x.Audio).ToArray();

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

        private void SetConfiguration(FileType fileType, Options options)
        {
            if (!options.KeepConfiguration)
            {
                Configuration = null;
            }

            if (!ContainerTypes.Containers.TryGetValue(fileType, out ContainerType type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), "Output type not in type dictionary");
            }
            OutType = type;

            Configuration = OutType.GetConfiguration(options, Configuration);
        }
    }
}
