using Cake.Common.Build;
using Cake.Common.IO;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build.Tasks
{
    [Dependency(typeof(Package))]
    public sealed class UploadAppVeyorArtifacts : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            foreach (FilePath file in context.GetFiles($"{context.PackageDir}/*"))
            {
                context.AppVeyor().UploadArtifact(file);
            }
        }

        public override bool ShouldRun(Context context) => context.BuildSystem().IsRunningOnAppVeyor;
    }
}
