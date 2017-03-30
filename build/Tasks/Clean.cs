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

            foreach (DirectoryPath path in directories)
            {
                DeleteDirectory(context, path, true);
            }

            FilePathCollection files = context.GetFiles($"{context.UwpDir}/_scale-*.appx");
            files += context.GetFiles($"{context.UwpDir}/*.nuget.props");
            files += context.GetFiles($"{context.UwpDir}/*.nuget.targets");
            files += context.GetFiles($"{context.UwpDir}/*.csproj.user");
            files += context.UwpDir.CombineWithFilePath("_pkginfo.txt");
            files += context.UwpDir.CombineWithFilePath("project.lock.json");
            files += context.UwpSideloadManifest;

            foreach (FilePath file in files)
            {
                DeleteFile(context, file, true);
            }
        }
    }

    public sealed class CleanBin : FrostingTask<Context>
    {
        public override void Run(Context context) => DeleteDirectory(context, context.BinDir, true);
    }

    public sealed class CleanTopBin : FrostingTask<Context>
    {
        public override void Run(Context context) => DeleteDirectory(context, context.TopBinDir, true);
    }

    public sealed class CleanPackage : FrostingTask<Context>
    {
        public override void Run(Context context) => DeleteDirectory(context, context.PackageDir, true);
    }
}
