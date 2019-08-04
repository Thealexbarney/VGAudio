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

        public static bool ConvertFile(Options options, JobFiles files, bool showProgress = false)
        {
            if (options.Job != JobType.Convert && options.Job != JobType.Batch) return false;

            var converter = new Convert { ShowProgress = showProgress };

            foreach (AudioFile file in files.InFiles)
            {
                converter.ReadFile(file);
            }

            converter.EncodeFiles(files, options);
            converter.SetConfiguration(files.OutFiles[0].Type, options);
            converter.WriteFile(files.OutFiles[0].Path, options);

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

        private void WriteFile(string fileName, Options options)
        {
            string directory = Path.GetDirectoryName(fileName);
            if (!String.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            bool isFuz = options.SkyrimFuz && options.NxOpusHeaderType == Containers.Opus.NxOpusHeaderType.Skyrim;
            int lipSize = 0, lipPadding = 0;
            byte[] lipData = { };

            if (isFuz)
            {
                string fileNameWithoutExtension = Path.GetDirectoryName(fileName) + Path.GetFileNameWithoutExtension(fileName);
                string lipFileName = fileNameWithoutExtension + ".LIP";
                fileName = fileNameWithoutExtension + ".FUZ";

                if (File.Exists(lipFileName))
                {
                    lipData = File.ReadAllBytes(lipFileName);
                    lipSize = lipData.Length;
                    lipPadding = lipSize % 4;
                    if (lipPadding != 0) lipPadding = 4 - lipPadding;
                }
            }

            using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite))
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
            using (var progress = new ProgressBar())
            {
                if (ShowProgress) Configuration.Progress = progress;
                if (isFuz)
                {
                    writer.Write(0x455A5546);
                    writer.Write((int)0x01);
                    writer.Write(lipSize);
                    writer.Write(0x10 + lipSize + lipPadding);
                    if (lipSize > 0) writer.Write(lipData);
                }
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
