using System.IO;
using Cake.Common.IO;
using Cake.Common.Tools.DotNetCore;
using Cake.Frosting;

namespace Build.SplitProject
{
    public sealed class Split : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            var root = context.SourceDir;
            var projects = new Projects(root);
            foreach (var project in projects.ProjectList)
            {
                context.EnsureDirectoryExists(project.Path);
                foreach (var source in project.Sources)
                {
                    context.EnsureDirectoryExists(source.Dest.Combine(".."));
                    context.EnsureDirectoryExists(source.Source);
                    context.MoveDirectory(source.Source, source.Dest);
                }
                context.DotNetCoreTool(context.SolutionFile, $"new classlib -f netstandard1.1 -o {project.Path}");
                context.DeleteFiles(project.Path.CombineWithFilePath("Class1.cs").ToString());

                context.DotNetCoreTool(project.Csproj, $"add reference {context.LibraryCsproj}");
                context.DotNetCoreTool(context.CliCsproj, $"add reference {project.Csproj}");
                context.DotNetCoreTool(context.ToolsCsproj, $"add reference {project.Csproj}");
                context.DotNetCoreTool(context.TestsCsproj, $"add reference {project.Csproj}");
                context.DotNetCoreTool(context.BenchmarkCsproj, $"add reference {project.Csproj}");
                context.DotNetCoreTool(context.SolutionFile, $"sln add {project.Csproj}");
                File.WriteAllText(project.Path.CombineWithFilePath("AssemblyInfo.cs").ToString(), InternalsVisibleTo);
            }

            foreach (var project in projects.ProjectList)
            {
                foreach (var dependency in project.Dependencies)
                {
                    context.DotNetCoreTool(project.Csproj, $"add reference {dependency.Csproj}");
                }
            }
        }

        private static readonly string InternalsVisibleTo =
            "using System.Runtime.CompilerServices;\n" +
            "[assembly: InternalsVisibleTo(\"VGAudio.Tests\")]\n" +
            "[assembly: InternalsVisibleTo(\"VGAudio.Benchmark\")]";
    }
}
