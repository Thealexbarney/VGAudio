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
            context.PackageDir = context.BaseDir.Combine("package");
            context.UwpDir = context.SourceDir.Combine("VGAudio.Uwp");
            context.CliPackageDir = context.PackageDir.Combine("VGAudioCli");
            context.BuildTargetsFile = context.SourceDir.CombineWithFilePath("Directory.Build.targets");
            context.TrimSolution = context.SourceDir.CombineWithFilePath("VGAudio.sln");

            context.AppxPublisher = "2E186599-2EB7-4677-93A5-C222C2F74D01";
            context.ReleaseCertThumbprint = "A81DF5034B182A7235B71B5524CCC9EE822BFA98";
        }
    }
}