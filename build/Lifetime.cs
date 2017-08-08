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
            context.PackageDir = context.BaseDir.Combine("package");

            context.LibraryDir = context.SourceDir.Combine("VGAudio");
            context.CliDir = context.SourceDir.Combine("VGAudio.Cli");
            context.TestsDir = context.SourceDir.Combine("VGAudio.Tests");
            context.ToolsDir = context.SourceDir.Combine("VGAudio.Tools");
            context.SourceDir.Combine("VGAudio.Benchmark");
            context.UwpDir = context.SourceDir.Combine("VGAudio.Uwp");
            context.BenchmarkDir = context.SourceDir.Combine("VGAudio.Benchmark");

            context.SolutionFile = context.SourceDir.CombineWithFilePath("VGAudio.sln");
            context.LibraryCsproj = context.LibraryDir.CombineWithFilePath("VGAudio.csproj");
            context.CliCsproj = context.CliDir.CombineWithFilePath("VGAudio.Cli.csproj");
            context.ToolsCsproj = context.ToolsDir.CombineWithFilePath("VGAudio.Tools.csproj");
            context.TestsCsproj = context.TestsDir.CombineWithFilePath("VGAudio.Tests.csproj");
            context.UwpCsproj = context.UwpDir.CombineWithFilePath("VGAudio.Uwp.csproj");
            context.BenchmarkCsproj = context.BenchmarkDir.CombineWithFilePath("VGAudio.Benchmark.csproj");

            context.TopBinDir = context.BaseDir.Combine("bin");
            context.BinDir = context.TopBinDir.Combine(context.IsReleaseBuild ? "release" : "debug");
            context.LibraryBinDir = context.BinDir.Combine("NuGet");
            context.CliBinDir = context.BinDir.Combine("cli");
            context.UwpBinDir = context.BinDir.Combine("uwp");

            context.UwpStoreManifest = context.UwpDir.CombineWithFilePath("Package.appxmanifest");
            context.UwpSideloadManifest = context.UwpDir.CombineWithFilePath("Sideload.appxmanifest");
            context.SideloadAppxName = "TheAlexBarney.VGAudio";
            context.AppxPublisher = "2E186599-2EB7-4677-93A5-C222C2F74D01";

            context.ReleaseCertThumbprint = "2043012AE523F7FA0F77A537387633BEB7A9F4DD";
        }
    }
}