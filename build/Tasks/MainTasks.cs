using Cake.Frosting;

namespace Build.Tasks
{
    [Dependency(typeof(Setup))]
    [Dependency(typeof(Build))]
    public sealed class Default : FrostingTask<Context> { }

    [Dependency(typeof(Clean))]
    [Dependency(typeof(RunDotnetBuild))]
    [Dependency(typeof(TestLibrary))]
    [Dependency(typeof(RunUwpBuild))]
    [Dependency(typeof(RunUwpStoreBuild))]
    public sealed class Build : FrostingTask<Context> { }

    [Dependency(typeof(SetupAll))]
    [Dependency(typeof(Build))]
    [Dependency(typeof(Sign))]
    public sealed class Release : FrostingTask<Context> { }

    public sealed class SetupAll : FrostingTask<Context>
    {
        public override void Run(Context context) => context.EnableAll();
    }

    public sealed class Setup : FrostingTask<Context>
    {
        public override void Run(Context context) => context.ParseArguments();
    }

    public sealed class Clean : FrostingTask<Context>
    {
        public override void Run(Context context) => Utilities.RunCoreMsBuild(context, "CleanBuilds");
        public override bool ShouldRun(Context context) => context.RunClean || context.RunCleanAll;
    }

    public sealed class TestLibrary : FrostingTask<Context>
    {
        public override void Run(Context context) => Utilities.RunCoreMsBuild(context, "TestLibrary");
        public override bool ShouldRun(Context context) => context.RunTests && (context.RunNetCore || context.RunNetFramework);
    }
}
