using System;
using Cake.Common;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Restore;
using Cake.Common.Tools.MSBuild;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using static Build.Utilities.Runners;

namespace Build.Tasks
{
    public sealed class Restore : FrostingTask<Context>
    {
        public override void Run(Context context) => context.DotNetCoreRestore(context.SourceDir.FullPath);
    }

    public sealed class RestoreUwp : FrostingTask<Context>
    {
        public override void Run(Context context) =>
            context.MSBuild(context.UwpDir.CombineWithFilePath("VGAudio.Uwp.csproj"), new MSBuildSettings
            {
                Targets = { "Restore" },
                Verbosity = Verbosity.Minimal
            });

        public override bool ShouldRun(Context context) => context.IsRunningOnWindows();

        public override void OnError(Exception exception, Context context) => DisplayError(context, exception.Message);
    }

    public sealed class RestoreTools : FrostingTask<Context>
    {
        public override void Run(Context context) =>
            context.DotNetCoreRestore(context.BuildDir.CombineWithFilePath("Tools.xml").FullPath, new DotNetCoreRestoreSettings
            {
                PackagesDirectory = context.CakeToolsDir,
                ArgumentCustomization = args => args.Append($"/p:RestoreOutputPath=\"{context.CakeToolsDir.Combine("obj")}\"")
            });
    }
}
