using System.Collections.Generic;
using System.Linq;
using Cake.Common.IO;
using Cake.Core.IO;
using Cake.Frosting;
using static Build.Utilities;

namespace Build.Tasks
{
    public sealed class Sign : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            SignTasks.SignAll(context);
            BuildTasks.PackageNonUwp(context);
        }

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
            FilePathCollection packages = context.GetFiles($"{context.LibraryBinDir}/*.nupkg");

            foreach (FilePath file in packages)
            {
                DirectoryPath extracted = context.LibraryBinDir.Combine(file.GetFilenameWithoutExtension().ToString());
                context.Unzip(file, extracted);

                FilePathCollection toSign = context.GetFiles($"{extracted}/lib/**/VGAudio.dll");
                SignFiles(context, toSign, context.ReleaseCertThumbprint);
                context.Zip(extracted, context.LibraryBinDir.CombineWithFilePath(file.GetFilename()));
                context.DeleteDirectory(extracted, new DeleteDirectorySettings
                {
                    Recursive = true
                });
            }
        }

        public static void SignCli(Context context)
        {
            var possibleNames = new[] { "VGAudio.dll", "VGAudioCli.exe", "VGAudioCli.dll", "VGAudioTools.exe", "VGAudioCli.dll" };

            List<FilePath> toSign = context.LibBuilds.Values
                .SelectMany(build => possibleNames
                    .Select(file => context.CliBinDir
                        .Combine(build.CliFramework)
                        .CombineWithFilePath(file)))
                .ToList();

            //Add merged assemblies
            toSign.Add(context.CliBinDir.CombineWithFilePath("VGAudioCli.exe"));
            toSign.Add(context.CliBinDir.CombineWithFilePath("VGAudioTools.exe"));

            SignFiles(context, toSign.Where(context.FileExists), context.ReleaseCertThumbprint);
        }

        public static void SignUwp(Context context)
        {
            SignFiles(context, context.GetFiles($"{context.PackageDir}/*.appxbundle"), context.ReleaseCertThumbprint);
        }
    }
}
