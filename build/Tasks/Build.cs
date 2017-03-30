using System;
using Cake.Common.Tools.MSBuild;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using static Build.Utilities.Runners;

namespace Build.Tasks
{
    [Dependency(typeof(BuildLibraryNetStandard))]
    [Dependency(typeof(BuildLibraryNet45))]
    public sealed class BuildLibrary : FrostingTask<Context> { }

    [Dependency(typeof(BuildCliNetCore))]
    [Dependency(typeof(BuildCliNet45))]
    public sealed class BuildCli : FrostingTask<Context> { }

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
            context.LibBuilds["net45"].LibSuccess == true;

        public override void OnError(Exception exception, Context context)
        {
            DisplayError(context, exception.Message);
            context.LibBuilds["net45"].CliSuccess = false;
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

            settings.WithProperty("AppxBuildType", "store");
            context.MSBuild(context.UwpDir.CombineWithFilePath("VGAudio.Uwp.csproj"), settings);

            //The second manifext MUST be written after the first build, otherwise incremental builds will mess stuff up
            CreateSideloadAppxmanifest(context);
            settings.WithProperty("AppxBuildType", "sideload");
            settings.WithProperty("PackageCertificateThumbprint", context.ReleaseCertThumbprint);
            context.MSBuild(context.UwpDir.CombineWithFilePath("VGAudio.Uwp.csproj"), settings);

            context.OtherBuilds["uwp"] = true;
        }

        public override void Finally(Context context) =>
            DeleteFile(context, context.UwpSideloadManifest, false);

        public override void OnError(Exception exception, Context context)
        {
            DisplayError(context, exception.Message);
            context.OtherBuilds["uwp"] = false;
        }
    }
}
