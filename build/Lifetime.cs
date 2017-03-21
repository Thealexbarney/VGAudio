using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Frosting;

namespace Build
{
    public sealed class Lifetime : FrostingLifetime<Context>
    {
        public override void Setup(Context context)
        {
            context.Configuration = context.Argument("configuration", "Release");
            context.BaseDir = context.Environment.WorkingDirectory;
            context.SourceDir = context.BaseDir.Combine("src");
            context.LibraryDir = context.SourceDir.Combine("VGAudio");
            context.SlnFile = context.SourceDir.CombineWithFilePath("VGAudio.sln");
        }

        public override void Teardown(Context context, ITeardownContext info)
        {
            context.Information("Tearing things down...");
        }
    }
}