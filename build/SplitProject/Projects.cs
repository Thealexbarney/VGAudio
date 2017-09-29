using System.Collections.Generic;
using System.Linq;
using Cake.Core.IO;

namespace Build.SplitProject
{
    public class Projects
    {
        private const string GcAdpcmCodec = nameof(GcAdpcmCodec);
        private const string CriAdxCodec = nameof(CriAdxCodec);
        private const string CriHcaCodec = nameof(CriHcaCodec);

        private const string Pcm8Format = nameof(Pcm8Format);
        private const string GcAdpcmFormat = nameof(GcAdpcmFormat);
        private const string CriAdxFormat = nameof(CriAdxFormat);
        private const string CriHcaFormat = nameof(CriHcaFormat);

        private const string Wave = nameof(Wave);
        private const string Dsp = nameof(Dsp);
        private const string NintendoWare = nameof(NintendoWare);
        private const string Idsp = nameof(Idsp);
        private const string Hps = nameof(Hps);
        private const string Genh = nameof(Genh);
        private const string Adx = nameof(Adx);
        private const string Hca = nameof(Hca);

        public List<Project> ProjectList { get; } = new List<Project>();

        private static readonly Dictionary<string, string[]> Dependencies = new Dictionary<string, string[]>
        {
            [GcAdpcmFormat] = new[] { GcAdpcmCodec },
            [CriAdxFormat] = new[] { CriAdxCodec },
            [CriHcaFormat] = new[] { CriHcaCodec },

            [Wave] = new[] { Pcm8Format },
            [Dsp] = new[] { GcAdpcmFormat },
            [NintendoWare] = new[] { GcAdpcmFormat, Pcm8Format },
            [Idsp] = new[] { GcAdpcmFormat },
            [Hps] = new[] { GcAdpcmFormat },
            [Genh] = new[] { GcAdpcmFormat },
            [Adx] = new[] { CriAdxFormat },
            [Hca] = new[] { CriHcaFormat },

        };

        private static readonly Dictionary<string, string[]> Directories = new Dictionary<string, string[]>
        {
            [GcAdpcmCodec] = new[] { "Codecs/GcAdpcm" },
            [CriAdxCodec] = new[] { "Codecs/CriAdx" },
            [CriHcaCodec] = new[] { "Codecs/CriHca" },

            [Pcm8Format] = new[] { "Formats/Pcm8", "Codecs/Pcm8" },
            [GcAdpcmFormat] = new[] { "Formats/GcAdpcm" },
            [CriAdxFormat] = new[] { "Formats/CriAdx" },
            [CriHcaFormat] = new[] { "Formats/CriHca" },

            [Wave] = new[] { "Containers/Wave" },
            [Dsp] = new[] { "Containers/Dsp" },
            [NintendoWare] = new[] { "Containers/NintendoWare" },
            [Idsp] = new[] { "Containers/Idsp" },
            [Hps] = new[] { "Containers/Hps" },
            [Genh] = new[] { "Containers/Genh" },
            [Adx] = new[] { "Containers/Adx" },
            [Hca] = new[] { "Containers/Hca" }
        };

        public Projects(DirectoryPath root)
        {
            foreach (var directory in Directories)
            {
                ProjectList.Add(new Project(directory.Key, directory.Value, root));
            }

            foreach (var project in ProjectList)
            {
                if (Dependencies.TryGetValue(project.Name, out var dependencies))
                {
                    project.Dependencies.AddRange(ProjectList.Where(x => dependencies.Contains(x.Name)));
                }
            }
        }
    }

    public class Project
    {
        public string Name { get; }
        public string FullName { get; }
        public DirectoryPath Path { get; }
        public FilePath Csproj { get; }
        public (DirectoryPath Source, DirectoryPath Dest)[] Sources { get; }
        public List<Project> Dependencies { get; } = new List<Project>();

        public Project(string name, string[] sources, DirectoryPath root)
        {
            Name = name;
            FullName = $"VGAudio.{name}";
            Path = root.Combine(FullName);
            Csproj = Path.CombineWithFilePath(FullName).AppendExtension("csproj");
            Sources = sources.Select(x => (root.Combine("VGAudio").Combine(x), Path.Combine(x))).ToArray();
        }
    }
}
