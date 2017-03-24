using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Restore;
using Cake.Core;
using Cake.Frosting;

namespace Build.Tasks
{
    public sealed class Restore : FrostingTask<Context>
    {
        public override void Run(Context context) => context.DotNetCoreRestore(context.SourceDir.FullPath);
    }

    public sealed class RestoreTools : FrostingTask<Context>
    {
        public override void Run(Context context) =>
            context.DotNetCoreRestore(context.BuildDir.CombineWithFilePath("Tools.xml").FullPath, new DotNetCoreRestoreSettings
            {
                PackagesDirectory = context.CakeToolsDir,
                ArgumentCustomization = args => args.Append($"/p:RestoreOutputPath=\"{context.CakeToolsDir.Combine("obj")}\"")
            });
    }
}
