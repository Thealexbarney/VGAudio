using System.Linq;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Build;
using Cake.Core;
using Cake.Frosting;
using static Build.Utilities;

namespace Build.Tasks
{
    public sealed class TestLibrary : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            string[] testFrameworks = context.TestFrameworks.ToArray();

            context.DotNetCoreBuild(context.TestsCsproj.FullPath, new DotNetCoreBuildSettings
            {
                Configuration = context.Configuration,
                ArgumentCustomization = args =>
                {
                    args.Append($"/p:TargetFrameworks=\\\"{string.Join(";", testFrameworks)}\\\"");
                    return args;
                }
            });

            foreach (var framework in testFrameworks)
            {
                TestNetCli(context, context.TestsCsproj.FullPath, framework);
            }
        }

        public override bool ShouldRun(Context context) => context.RunTests && context.TestFrameworks.Any();
    }
}
