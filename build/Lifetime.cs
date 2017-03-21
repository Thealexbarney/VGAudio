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
            context.CliDir = context.SourceDir.Combine("VGAudio.Cli");
            context.TestsDir = context.SourceDir.Combine("VGAudio.Tests");

            context.SlnFile = context.SourceDir.CombineWithFilePath("VGAudio.sln");
            context.TestsCsproj = context.TestsDir.CombineWithFilePath("VGAudio.Tests.csproj");

            context.PublishDir = context.BaseDir.Combine("Publish");
            context.LibraryPublishDir = context.PublishDir.Combine("NuGet");
            context.CliPublishDir = context.PublishDir.Combine("cli");
        }

        public override void Teardown(Context context, ITeardownContext info)
        {
            context.Information("Tearing things down...");
        }
    }
}