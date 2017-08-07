using System;
using Cake.Frosting;
using static Build.Utilities;

namespace Build.Tasks
{
    [Dependency(typeof(Restore))]
    [Dependency(typeof(BuildLibraryNetStandard))]
    public sealed class TestLibraryNetStandard : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            TestNetCli(context, context.TestsCsproj.FullPath, context.LibBuilds["core"].TestFramework);
            context.LibBuilds["core"].TestSuccess = true;
        }

        public override bool ShouldRun(Context context) =>
            context.LibBuilds["core"].LibSuccess == true;

        public override void OnError(Exception exception, Context context)
        {
            DisplayError(context, exception.Message);
            context.LibBuilds["core"].TestSuccess = false;
        }
    }

    [Dependency(typeof(Restore))]
    [Dependency(typeof(BuildLibraryNet45))]
    public sealed class TestLibraryNet45 : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            TestNetCli(context, context.TestsCsproj.FullPath, context.LibBuilds["full"].TestFramework);
            context.LibBuilds["full"].TestSuccess = true;
        }

        public override bool ShouldRun(Context context) =>
            context.LibBuilds["full"].LibSuccess == true;

        public override void OnError(Exception exception, Context context)
        {
            DisplayError(context, exception.Message);
            context.LibBuilds["full"].TestSuccess = false;
        }
    }
}
