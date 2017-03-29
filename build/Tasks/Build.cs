using System;
using Cake.Common.Tools.MSBuild;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using static Build.Utilities.Runners;

namespace Build.Tasks
{
    [Dependency(typeof(Restore))]
    public sealed class BuildLibraryNetStandard : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            BuildNetCli(context, context.LibraryDir.FullPath, "netstandard1.1");
            context.LibBuilds["netstandard"].LibSuccess = true;
        }

        public override void OnError(Exception exception, Context context) =>
            context.LibBuilds["netstandard"].LibSuccess = false;
    }

    [Dependency(typeof(Restore))]
    public sealed class BuildLibraryNet45 : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            BuildNetCli(context, context.LibraryDir.FullPath, "net45");
            context.LibBuilds["net45"].LibSuccess = true;
        }

        public override void OnError(Exception exception, Context context) =>
            context.LibBuilds["net45"].LibSuccess = false;
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

        public override void OnError(Exception exception, Context context) =>
            context.LibBuilds["netstandard"].CliSuccess = false;
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

        public override void OnError(Exception exception, Context context) =>
            context.LibBuilds["net45"].CliSuccess = false;
    }

    [Dependency(typeof(RestoreUwp))]
    public sealed class BuildUwp : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            string thumbprint = SetupUwpSigningCertificate(context);
            var settings = new MSBuildSettings
            {
                Verbosity = Verbosity.Minimal,
                MSBuildPlatform = MSBuildPlatform.x86,
                Configuration = context.Configuration
            };

            if (thumbprint != null)
            {
                settings.WithProperty("PackageCertificateThumbprint", thumbprint);
            }

            context.MSBuild(context.UwpDir.CombineWithFilePath("VGAudio.Uwp.csproj"), settings);
            context.OtherBuilds["uwp"] = true;
        }

        public override void OnError(Exception exception, Context context) =>
            context.OtherBuilds["uwp"] = false;
    }
}
