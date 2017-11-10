using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.MSBuild;
using Cake.Frosting;

namespace Build.Tasks
{
    public sealed class TestLibrary : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            var settings = new DotNetCoreMSBuildSettings { Targets = { "TestLibrary" } };
            BuildTasks.SetMsBuildProps(context, settings);
            context.DotNetCoreMSBuild(context.TestsCsproj.FullPath, settings);
        }

        public override bool ShouldRun(Context context) => context.RunTests && (context.RunNetCore || context.RunNetFramework);
    }
}
