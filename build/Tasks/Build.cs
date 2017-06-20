using System;
using Cake.Common;
using Cake.Common.Tools.MSBuild;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using static Build.Utilities;

namespace Build.Tasks
{
    [Dependency(typeof(BuildLibraryNetStandard))]
    [Dependency(typeof(BuildLibraryNet45))]
    public sealed class BuildLibrary : FrostingTask<Context> { }

    [Dependency(typeof(BuildCliNetCore))]
    [Dependency(typeof(BuildCliNet45))]
    public sealed class BuildCli : FrostingTask<Context> { }

    [Dependency(typeof(BuildToolsNetCore))]
    [Dependency(typeof(BuildToolsNet45))]
    public sealed class BuildTools : FrostingTask<Context> { }

    [Dependency(typeof(Restore))]
    public sealed class BuildLibraryNetStandard : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            BuildNetCli(context, context.LibraryDir.FullPath, "netstandard1.1");
            context.LibBuilds["netstandard"].LibSuccess = true;
        }

        public override void OnError(Exception exception, Context context)
        {
            DisplayError(context, exception.Message);
            context.LibBuilds["netstandard"].LibSuccess = false;
        }
    }

    [Dependency(typeof(Restore))]
    public sealed class BuildLibraryNet45 : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            BuildNetCli(context, context.LibraryDir.FullPath, "net45");
            context.LibBuilds["net45"].LibSuccess = true;
        }

        public override bool ShouldRun(Context context) => context.IsRunningOnWindows();

        public override void OnError(Exception exception, Context context)
        {
            DisplayError(context, exception.Message);
            context.LibBuilds["net45"].LibSuccess = false;
        }
    }

    [Dependency(typeof(BuildLibraryNetStandard))]
    public sealed class BuildCliNetCore : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            BuildNetCli(context, context.CliDir.FullPath, "netcoreapp1.0");
            context.LibBuilds["netstandard"].CliSuccess = true;
        }

        public override bool ShouldRun(Context context) =>
            context.LibBuilds["netstandard"].LibSuccess == true;

        public override void OnError(Exception exception, Context context)
        {
            DisplayError(context, exception.Message);
            context.LibBuilds["netstandard"].CliSuccess = false;
        }
    }

    [Dependency(typeof(BuildLibraryNet45))]
    public sealed class BuildCliNet45 : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            BuildNetCli(context, context.CliDir.FullPath, "net45");
            context.LibBuilds["net45"].CliSuccess = true;
        }

        public override bool ShouldRun(Context context) =>
            context.LibBuilds["net45"].LibSuccess == true &&
            context.IsRunningOnWindows();

        public override void OnError(Exception exception, Context context)
        {
            DisplayError(context, exception.Message);
            context.LibBuilds["net45"].CliSuccess = false;
        }
    }

    [Dependency(typeof(BuildLibraryNetStandard))]
    public sealed class BuildToolsNetCore : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            BuildNetCli(context, context.ToolsDir.FullPath, "netcoreapp1.0");
            context.LibBuilds["netstandard"].ToolsSuccess = true;
        }

        public override bool ShouldRun(Context context) =>
            context.LibBuilds["netstandard"].LibSuccess == true;

        public override void OnError(Exception exception, Context context)
        {
            DisplayError(context, exception.Message);
            context.LibBuilds["netstandard"].ToolsSuccess = false;
        }
    }

    [Dependency(typeof(BuildLibraryNet45))]
    public sealed class BuildToolsNet45 : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            BuildNetCli(context, context.ToolsDir.FullPath, "net45");
            context.LibBuilds["net45"].ToolsSuccess = true;
        }

        public override bool ShouldRun(Context context) =>
            context.LibBuilds["net45"].LibSuccess == true &&
            context.IsRunningOnWindows();

        public override void OnError(Exception exception, Context context)
        {
            DisplayError(context, exception.Message);
            context.LibBuilds["net45"].ToolsSuccess = false;
        }
    }

    [Dependency(typeof(RestoreUwp))]
    public sealed class BuildUwp : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            SetupUwpSigningCertificate(context);

            var settings = new MSBuildSettings
            {
                Verbosity = Verbosity.Minimal,
                MSBuildPlatform = MSBuildPlatform.x86,
                Configuration = context.Configuration
            };

            settings.WithProperty("VisualStudioVersion", "15.0");

            context.MSBuild(context.UwpCsproj, settings.WithProperty("AppxBuildType", "Store"));

            //The second manifest MUST be written after the first build, otherwise incremental builds will mess stuff up
            CreateSideloadAppxmanifest(context);
            context.MSBuild(context.UwpCsproj, settings.WithProperty("AppxBuildType", "Sideload"));

            context.OtherBuilds["uwp"] = true;
        }

        public override bool ShouldRun(Context context) => context.IsRunningOnWindows();

        public override void Finally(Context context) =>
            DeleteFile(context, context.UwpSideloadManifest, false);

        public override void OnError(Exception exception, Context context)
        {
            DisplayError(context, exception.Message);
            context.OtherBuilds["uwp"] = false;
        }
    }
}
