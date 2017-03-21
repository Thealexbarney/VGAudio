using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Build;
using Cake.Frosting;

namespace Build.Tasks
{
    [Dependency(typeof(BuildLibraryNetStandard))]
    public sealed class BuildLibrary : FrostingTask<Context>
    {
    }

    [Dependency(typeof(Restore))]
    public sealed class BuildLibraryNetStandard : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            context.DotNetCoreBuild(context.LibraryDir.FullPath, new DotNetCoreBuildSettings
            {
                Framework = "netstandard1.1",
                Configuration = context.Configuration
            });
        }
    }
}
