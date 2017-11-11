using System.Linq;
using Cake.Common;
using Cake.Common.Build;
using Cake.Common.IO;
using Cake.Common.Tools.MSBuild;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Frosting;
using ILRepacking;
using static Build.Utilities;

namespace Build.Tasks
{
    public sealed class RunDotnetBuild : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            RunCoreMsBuild(context, "BuildDotnet", "PublishDotnet");

            if (context.RunNetFramework)
            {
                BuildTasks.IlMergeCli(context);
            }

            context.Zip(context.CliPackageDir, context.PackageDir.CombineWithFilePath("VGAudioCli.zip"));
            context.DeleteDirectory(context.CliPackageDir, false);

            if (context.BuildSystem().IsRunningOnAppVeyor)
            {
                FilePathCollection uploadFiles = context.GetFiles($"{context.PackageDir}/*.nupkg");
                uploadFiles.Add(context.GetFiles($"{context.PackageDir}/*.exe"));
                uploadFiles.Add(context.GetFiles($"{context.PackageDir}/*.zip"));

                foreach (FilePath file in uploadFiles)
                {
                    context.AppVeyor().UploadArtifact(file);
                }
            }
        }

        public override bool ShouldRun(Context context) =>
           context.RunBuild && (context.RunNetCore || context.RunNetFramework);
    }

    public sealed class RestoreUwp : FrostingTask<Context>
    {
        public override void Run(Context context) =>
            context.MSBuild(context.BuildTargetsFile, new MSBuildSettings
            {
                Targets = { "RestoreUwp" },
                Verbosity = Verbosity.Minimal
            });

        public override bool ShouldRun(Context context) => context.IsRunningOnWindows() && (context.BuildUwp || context.BuildUwpStore);
    }

    [Dependency(typeof(RestoreUwp))]
    public sealed class RunUwpBuild : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            BuildTasks.BuildUwp(context);

            if (context.BuildSystem().IsRunningOnAppVeyor)
            {
                foreach (FilePath file in context.GetFiles($"{context.PackageDir}/*.appxbundle"))
                {
                    context.AppVeyor().UploadArtifact(file);
                }
            }
        }

        public override bool ShouldRun(Context context) => context.IsRunningOnWindows() && context.BuildUwp;
    }

    [Dependency(typeof(RestoreUwp))]
    public sealed class RunUwpStoreBuild : FrostingTask<Context>
    {
        public override void Run(Context context) => BuildTasks.BuildUwp(context, true);
        public override bool ShouldRun(Context context) => context.IsRunningOnWindows() && context.BuildUwpStore;
    }

    public static class BuildTasks
    {
        public static void BuildUwp(Context context, bool storeBuild = false)
        {
            SetupUwpSigningCertificate(context);

            var settings = new MSBuildSettings
            {
                Verbosity = Verbosity.Minimal,
                MSBuildPlatform = MSBuildPlatform.x86,
                Configuration = context.Configuration,
                Targets = { storeBuild ? "BuildUwpStore" : "BuildUwpSideload" }
            };

            context.MSBuild(context.BuildTargetsFile, settings);
        }

        public static void IlMergeCli(Context context)
        {
            var inDir = context.GetFiles($"{context.CliPackageDir}/**/*.exe").FirstOrDefault()?.GetDirectory();
            if (inDir == null) return;
            var outDir = context.CliPackageDir.Combine($"{inDir.GetDirectoryName()}_standalone");

            string cliPath = inDir.CombineWithFilePath("VGAudioCli.exe").FullPath;
            string libPath = inDir.CombineWithFilePath("VGAudio.dll").FullPath;
            string toolsPath = inDir.CombineWithFilePath("VGAudioTools.exe").FullPath;
            context.EnsureDirectoryExists(outDir);

            var cliOptions = new RepackOptions
            {
                OutputFile = outDir.CombineWithFilePath("VGAudioCli.exe").FullPath,
                InputAssemblies = new[] { cliPath, libPath },
                SearchDirectories = new[] { "." }
            };

            var toolsOptions = new RepackOptions
            {
                OutputFile = outDir.CombineWithFilePath("VGAudioTools.exe").FullPath,
                InputAssemblies = new[] { toolsPath, libPath },
                SearchDirectories = new[] { "." }
            };

            new ILRepack(cliOptions).Repack();
            new ILRepack(toolsOptions).Repack();
            context.CopyFiles(context.GetFiles($"{outDir}/*.exe"), context.PackageDir);
        }
    }
}
