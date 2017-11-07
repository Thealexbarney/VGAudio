using Cake.Common.IO;
using Cake.Core.IO;
using Cake.Frosting;
using static Build.Utilities;

namespace Build.Tasks
{
    public sealed class Clean : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            context.CleanBuild();
            context.CleanBin();
            if (context.RunCleanAll)
            {
                context.CleanTopBin();
                context.CleanPackage();
            }
        }

        public override bool ShouldRun(Context context) => context.RunClean || context.RunCleanAll;
    }

    public static class CleanTasks
    {
        public static void CleanBuild(this Context context)
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
            files += context.GetFiles($"{context.UwpDir}/*.csproj.user");
            files += context.UwpDir.CombineWithFilePath("_pkginfo.txt");
            files += context.UwpSideloadManifest;

            foreach (FilePath file in files)
            {
                DeleteFile(context, file, true);
            }
        }

        public static void CleanBin(this Context context) => DeleteDirectory(context, context.BinDir, true);
        public static void CleanTopBin(this Context context) => DeleteDirectory(context, context.TopBinDir, true);
        public static void CleanPackage(this Context context) => DeleteDirectory(context, context.PackageDir, true);
    }
}
