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

            context.BuildTargetsFile = context.SourceDir.CombineWithFilePath("Directory.Build.targets");

            context.TopBinDir = context.BaseDir.Combine("bin");
            context.BinDir = context.TopBinDir.Combine(context.Configuration);
            context.CliBinDir = context.BinDir.Combine("cli");

            context.AppxPublisher = "2E186599-2EB7-4677-93A5-C222C2F74D01";
            context.ReleaseCertThumbprint = "2043012AE523F7FA0F77A537387633BEB7A9F4DD";
        }
    }
}