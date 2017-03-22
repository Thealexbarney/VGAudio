using System.Collections.Generic;
using System.Linq;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Pack;
using Cake.Common.Tools.DotNetCore.Publish;
using Cake.Core;
using Cake.Frosting;

namespace Build.Tasks
{
    [Dependency(typeof(BuildLibrary))]
    public sealed class PublishLibrary : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            string[] frameworks = context.LibBuilds.Values
                .Where(x => x.LibSuccess == true)
                .Select(x => x.LibFramework)
                .ToArray();

            context.DotNetCorePack(context.LibraryDir.FullPath, new DotNetCorePackSettings
            {
                Configuration = context.Configuration,
                OutputDirectory = context.LibraryPublishDir,
                NoBuild = true,
                ArgumentCustomization = args =>
                {
                    args.Append("--include-source");
                    args.Append("--include-symbols");
                    return args.Append($"/p:TargetFrameworks=\\\"{string.Join(";", frameworks)}\\\"");
                }
            });
        }

        public override bool ShouldRun(Context context) =>
            context.LibBuilds.Values.Any(x => x.LibSuccess == true);
    }

    [Dependency(typeof(BuildCli))]
    public sealed class PublishCli : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            IEnumerable<LibraryBuildStatus> builds = context.LibBuilds.Values
                .Where(x => x.CliSuccess == true);

            foreach (LibraryBuildStatus build in builds)
            {
                context.DotNetCorePublish(context.CliDir.FullPath, new DotNetCorePublishSettings
                {
                    Framework = build.CliFramework,
                    Configuration = context.Configuration,
                    OutputDirectory = context.CliPublishDir.Combine(build.CliFramework)
                });
            }
        }

        public override bool ShouldRun(Context context) =>
            context.LibBuilds.Values.Any(x => x.CliSuccess == true);
    }
}
