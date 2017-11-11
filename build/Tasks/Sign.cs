using System.Linq;
using Cake.Common.IO;
using Cake.Core.IO;
using Cake.Frosting;
using static Build.Utilities;

namespace Build.Tasks
{
    public sealed class Sign : FrostingTask<Context>
    {
        public override void Run(Context context) => SignTasks.SignAll(context);
        public override bool ShouldRun(Context context) => CertificateExists(context.ReleaseCertThumbprint, true);
    }

    public static class SignTasks
    {
        public static void SignAll(Context context)
        {
            SignLibrary(context);
            SignCli(context);
            SignUwp(context);
        }

        public static void SignLibrary(Context context)
        {
            FilePathCollection packages = context.GetFiles($"{context.PackageDir}/*.nupkg");

            foreach (FilePath file in packages)
            {
                DirectoryPath extracted = context.PackageDir.Combine(file.GetFilenameWithoutExtension().ToString());
                context.DeleteDirectory(extracted, false);
                context.Unzip(file, extracted);

                FilePathCollection toSign = context.GetFiles($"{extracted}/lib/**/VGAudio.dll");
                SignFiles(context, toSign, context.ReleaseCertThumbprint);
                context.Zip(extracted, context.PackageDir.CombineWithFilePath(file.GetFilename()));
                context.DeleteDirectory(extracted, false);
            }
        }

        public static void SignCli(Context context)
        {
            FilePath file = context.PackageDir.CombineWithFilePath("VGAudioCli.zip");
            DirectoryPath extracted = context.PackageDir.Combine(file.GetFilenameWithoutExtension().ToString());
            context.DeleteDirectory(extracted, false);
            context.Unzip(file, extracted);

            FilePathCollection toSign = context.GetFiles($"{extracted}/**/VGAudio*.exe");
            toSign.Add(context.GetFiles($"{extracted}/**/VGAudio*.dll"));
            toSign.Add(context.PackageDir.CombineWithFilePath("VGAudioCli.exe"));
            toSign.Add(context.PackageDir.CombineWithFilePath("VGAudioTools.exe"));

            SignFiles(context, toSign.Where(context.FileExists), context.ReleaseCertThumbprint);
            context.Zip(extracted, context.PackageDir.CombineWithFilePath(file.GetFilename()));
            context.DeleteDirectory(extracted, false);
        }

        public static void SignUwp(Context context)
        {
            SignFiles(context, context.GetFiles($"{context.PackageDir}/*.appxbundle"), context.ReleaseCertThumbprint);
        }
    }
}
