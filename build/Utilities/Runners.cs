using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Build;
using Cake.Common.Tools.DotNetCore.Test;

namespace Build.Utilities
{
    internal static class Runners
    {
        public static void BuildNetCli(Context context, string path, string framework)
        {
            context.DotNetCoreBuild(path, new DotNetCoreBuildSettings
            {
                Framework = framework,
                Configuration = context.Configuration
            });
        }

        public static void TestNetCli(Context context, string csprojPath, string framework)
        {
            context.DotNetCoreTest(csprojPath, new DotNetCoreTestSettings
            {
                Framework = framework,
                Configuration = context.Configuration
            });
        }
    }
}
