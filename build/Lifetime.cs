using Cake.Common;
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
            context.BuildDir = context.BaseDir.Combine("build");
            context.CakeToolsDir = context.BaseDir.Combine("tools/cake");

            context.LibraryDir = context.SourceDir.Combine("VGAudio");
            context.CliDir = context.SourceDir.Combine("VGAudio.Cli");
            context.TestsDir = context.SourceDir.Combine("VGAudio.Tests");
            context.BenchmarkDir = context.SourceDir.Combine("VGAudio.Benchmark");
            context.UwpDir = context.SourceDir.Combine("VGAudio.Uwp");

            context.SlnFile = context.SourceDir.CombineWithFilePath("VGAudio.sln");
            context.TestsCsproj = context.TestsDir.CombineWithFilePath("VGAudio.Tests.csproj");

            context.PublishDir = context.BaseDir.Combine("Publish");
            context.LibraryPublishDir = context.PublishDir.Combine("NuGet");
            context.CliPublishDir = context.PublishDir.Combine("cli");

            context.ReleaseCertThumbprint = "2043012AE523F7FA0F77A537387633BEB7A9F4DD";
        }
    }
}