using Cake.Common.IO;
using Cake.Frosting;

namespace Build.Tasks
{
    [Dependency(typeof(PublishLibrary))]
    public sealed class PackageLibrary : FrostingTask<Context>
    {
        public override void Run(Context context) => context.CopyDirectory(context.LibraryBinDir, context.PackageDir);

        public override bool ShouldRun(Context context) => context.LibraryBuildsSucceeded;
    }

    [Dependency(typeof(PublishCli))]
    public sealed class PackageCli : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            context.EnsureDirectoryExists(context.PackageDir);
            context.Zip(context.CliBinDir, context.PackageDir.CombineWithFilePath("VGAudioCli.zip"));
        }

        public override bool ShouldRun(Context context) => context.CliBuildsSucceeded;
    }

    [Dependency(typeof(PublishUwp))]
    public sealed class PackageUwp : FrostingTask<Context>
    {
        public override void Run(Context context) =>
            context.CopyFiles(context.GetFiles($"{context.UwpBinDir}/*.appxbundle"), context.PackageDir);

        public override bool ShouldRun(Context context) => context.OtherBuilds["uwp"] == true;
    }
}
