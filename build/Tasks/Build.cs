using Cake.Common;
using Cake.Common.Build;
using Cake.Common.IO;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.MSBuild;
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
            BuildTasks.RunDotnetBuild(context);

            if (context.RunNetFramework)
            {
                BuildTasks.IlMergeCli(context);
            }

            context.CopyFiles(context.GetFiles($"{context.CliBinDir}/*.exe"), context.PackageDir);
            context.Zip(context.CliBinDir, context.PackageDir.CombineWithFilePath("VGAudioCli.zip"));

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
            context.MSBuild(context.UwpCsproj, new MSBuildSettings
            {
                Targets = { "Restore" },
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
        public static void RunDotnetBuild(Context context)
        {
            var settings = new DotNetCoreMSBuildSettings
            {
                Targets = { "BuildDotnet", "PublishDotnet" }
            };

            settings.Properties.Add("DoBuild", new[] { context.RunBuild ? "true" : "false" });
            settings.Properties.Add("DoTest", new[] { context.RunTests ? "true" : "false" });
            settings.Properties.Add("DoClean", new[] { context.RunClean ? "true" : "false" });
            settings.Properties.Add("DoNetCore", new[] { context.RunNetCore ? "true" : "false" });
            settings.Properties.Add("DoNetFramework", new[] { context.RunNetFramework ? "true" : "false" });

            settings.Properties.Add("Configuration", new[] { context.Configuration });
            context.DotNetCoreMSBuild(context.LibraryCsproj.FullPath, settings);
        }

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

            context.MSBuild(context.UwpCsproj, settings);
        }

        public static void IlMergeCli(Context context)
        {
            string cliPath = context.CliBinDir.CombineWithFilePath($"{context.LibBuilds["full"].CliFramework}/VGAudioCli.exe").FullPath;
            string libPath = context.CliBinDir.CombineWithFilePath($"{context.LibBuilds["full"].CliFramework}/VGAudio.dll").FullPath;
            string toolsPath = context.CliBinDir.CombineWithFilePath($"{context.LibBuilds["full"].ToolsFramework}/VGAudioTools.exe").FullPath;

            var cliOptions = new RepackOptions
            {
                OutputFile = context.CliBinDir.CombineWithFilePath("VGAudioCli.exe").FullPath,
                InputAssemblies = new[] { cliPath, libPath },
                SearchDirectories = new[] { "." }
            };

            var toolsOptions = new RepackOptions
            {
                OutputFile = context.CliBinDir.CombineWithFilePath("VGAudioTools.exe").FullPath,
                InputAssemblies = new[] { toolsPath, libPath },
                SearchDirectories = new[] { "." }
            };

            new ILRepack(cliOptions).Repack();
            new ILRepack(toolsOptions).Repack();
        }
    }
}
