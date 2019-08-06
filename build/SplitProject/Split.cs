using System.IO;
using System.Linq;
using Cake.Common.IO;
using Cake.Common.Tools.DotNetCore;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build.SplitProject
{
    public sealed class Split : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            DirectoryPath root = context.SourceDir;
            FilePath solutionFile = context.SourceDir.CombineWithFilePath("VGAudio.sln");
            FilePath libraryProject = context.SourceDir.CombineWithFilePath("VGAudio/VGAudio.csproj");
            var projects = new Projects(root);

            foreach (Project project in projects.ProjectList)
            {
                context.EnsureDirectoryExists(project.Path);
                foreach (var source in project.Sources)
                {
                    context.EnsureDirectoryExists(source.Dest.Combine(".."));
                    context.EnsureDirectoryExists(source.Source);
                    context.MoveDirectory(source.Source, source.Dest);
                }

                File.WriteAllText(project.Csproj.FullPath, DefaultProjectFile);
                File.WriteAllText(project.Path.CombineWithFilePath("AssemblyInfo.cs").ToString(), InternalsVisibleTo);
            }

            string[] newProjects = projects.ProjectList.Select(x => x.Csproj.FullPath).ToArray();
            string references = string.Join(' ', newProjects);
            context.DotNetCoreTool(solutionFile, $"sln add {references}");

            foreach (string project in OldProjects)
            {
                context.DotNetCoreTool(context.SourceDir.CombineWithFilePath(project), $"add reference {references}");
            }

            foreach (Project project in projects.ProjectList)
            {
                string dependencies = string.Join(' ', project.Dependencies.Select(x => x.Csproj.FullPath).ToArray());
                context.DotNetCoreTool(project.Csproj, $"add reference {dependencies} {libraryProject}");
            }
        }

        private static readonly string InternalsVisibleTo =
            "using System.Runtime.CompilerServices;\n" +
            "[assembly: InternalsVisibleTo(\"VGAudio.Tests\")]\n" +
            "[assembly: InternalsVisibleTo(\"VGAudio.Benchmark\")]";

        private static readonly string DefaultProjectFile =
            @"<Project Sdk=""Microsoft.NET.Sdk""><PropertyGroup><TargetFrameworks>netstandard1.3;net45</TargetFrameworks></PropertyGroup></Project>";

        private static readonly string[] OldProjects =
        {
            "VGAudio.Cli/VGAudio.Cli.csproj",
            "VGAudio.Tools/VGAudio.Tools.csproj",
            "VGAudio.Tests/VGAudio.Tests.csproj",
            "VGAudio.Benchmark/VGAudio.Benchmark.csproj"
        };
    }
}
