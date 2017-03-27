using System.Linq;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Core.IO;
using Cake.Frosting;
using static Build.Utilities.Runners;

namespace Build.Tasks
{
    public sealed class CleanBuild : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            DirectoryPathCollection directories = context.GetDirectories($"{context.SourceDir}/**/obj");
            directories += context.GetDirectories($"{context.SourceDir}/**/bin");
            directories += context.UwpDir.Combine("AppPackages");
            directories += context.UwpDir.Combine("BundleArtifacts");

            foreach (DirectoryPath path in directories.Where(context.DirectoryExists))
            {
                context.Information($"Deleting {path}");
                context.DeleteDirectory(path, true);
            }

            FilePathCollection files = context.GetFiles($"{context.UwpDir}/_scale-*.appx");
            files += context.GetFiles($"{context.UwpDir}/*.nuget.props");
            files += context.GetFiles($"{context.UwpDir}/*.nuget.targets");
            files += context.GetFiles($"{context.UwpDir}/*.csproj.user");
            files += context.UwpDir.CombineWithFilePath("_pkginfo.txt");
            files += context.UwpDir.CombineWithFilePath("project.lock.json");

            foreach (FilePath file in files.Where(context.FileExists))
            {
                context.Information($"Deleting {file}");
                context.DeleteFile(file);
            }
        }
    }

    public sealed class CleanPublish : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            DeleteDirectory(context, context.PublishDir);
        }
    }
}
