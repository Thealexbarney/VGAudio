using System;
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

        public override bool ShouldRun(Context context) =>
            context.LibBuilds["netstandard"].LibSuccess == null;

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

        public override bool ShouldRun(Context context) =>
            context.LibBuilds["net45"].LibSuccess == null;

        public override void OnError(Exception exception, Context context) =>
            context.LibBuilds["net45"].LibSuccess = false;
    }

    [Dependency(typeof(Restore))]
    public sealed class BuildCliNetCore : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            BuildNetCli(context, context.CliDir.FullPath, "netcoreapp1.0");
            context.LibBuilds["netstandard"].CliSuccess = true;
            context.LibBuilds["netstandard"].LibSuccess = true;
        }

        public override bool ShouldRun(Context context) =>
            context.LibBuilds["netstandard"].CliSuccess == null &&
            context.LibBuilds["netstandard"].LibSuccess != false;

        public override void OnError(Exception exception, Context context) =>
            context.LibBuilds["netstandard"].CliSuccess = false;
    }

    [Dependency(typeof(Restore))]
    public sealed class BuildCliNet45 : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            BuildNetCli(context, context.CliDir.FullPath, "net45");
            context.LibBuilds["net45"].CliSuccess = true;
            context.LibBuilds["net45"].LibSuccess = true;
        }

        public override bool ShouldRun(Context context) =>
            context.LibBuilds["net45"].CliSuccess == null &&
            context.LibBuilds["net45"].LibSuccess != false;

        public override void OnError(Exception exception, Context context) =>
            context.LibBuilds["net45"].CliSuccess = false;
    }
}
