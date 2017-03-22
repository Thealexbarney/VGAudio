using Cake.Common.IO;
using Cake.Core.IO;
using Cake.Frosting;
using static Build.Utilities.Runners;

namespace Build.Tasks
{
    public sealed class CleanBuild : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            DirectoryPathCollection directories = context.GetDirectories($"{context.SourceDir}/**/obj");
            directories += context.GetDirectories($"{context.SourceDir}/**/bin");

            foreach (DirectoryPath path in directories)
            {
                context.DeleteDirectory(path, true);
            }
        }
    }

    public sealed class CleanPublish : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            DeleteDirectory(context, context.PublishDir);
        }
    }
}
