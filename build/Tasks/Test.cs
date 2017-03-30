using System;
using Cake.Frosting;
using static Build.Utilities.Runners;

namespace Build.Tasks
{
    [Dependency(typeof(Restore))]
    [Dependency(typeof(BuildLibraryNetStandard))]
    public sealed class TestLibraryNetStandard : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            TestNetCli(context, context.TestsCsproj.FullPath, "netcoreapp1.0");
            context.LibBuilds["netstandard"].TestSuccess = true;
        }

        public override bool ShouldRun(Context context) =>
            context.LibBuilds["netstandard"].LibSuccess == true;

        public override void OnError(Exception exception, Context context)
        {
            DisplayError(context, exception.Message);
            context.LibBuilds["netstandard"].TestSuccess = false;
        }
    }

    [Dependency(typeof(Restore))]
    [Dependency(typeof(BuildLibraryNet45))]
    public sealed class TestLibraryNet45 : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            TestNetCli(context, context.TestsCsproj.FullPath, "net46");
            context.LibBuilds["net45"].TestSuccess = true;
        }

        public override bool ShouldRun(Context context) =>
            context.LibBuilds["net45"].LibSuccess == true;

        public override void OnError(Exception exception, Context context)
        {
            DisplayError(context, exception.Message);
            context.LibBuilds["net45"].TestSuccess = false;
        }
    }
}
