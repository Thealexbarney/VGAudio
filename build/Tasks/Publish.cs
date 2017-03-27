using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Pack;
using Cake.Common.Tools.DotNetCore.Publish;
using Cake.Common.Tools.ILRepack;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build.Tasks
{
    [Dependency(typeof(BuildLibrary))]
    public sealed class PublishLibrary : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            string[] frameworks = context.LibBuilds.Values
                .Where(x => x.LibSuccess == true)
                .Select(x => x.LibFramework)
                .ToArray();

            context.DotNetCorePack(context.LibraryDir.FullPath, new DotNetCorePackSettings
            {
                Configuration = context.Configuration,
                OutputDirectory = context.LibraryPublishDir,
                NoBuild = true,
                ArgumentCustomization = args =>
                {
                    args.Append("--include-source");
                    args.Append("--include-symbols");
                    return args.Append($"/p:TargetFrameworks=\\\"{string.Join(";", frameworks)}\\\"");
                }
            });
        }

        public override bool ShouldRun(Context context) =>
            context.LibBuilds.Values.Any(x => x.LibSuccess == true);
    }

    [Dependency(typeof(BuildCli))]
    public sealed class PublishCli : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            IEnumerable<LibraryBuildStatus> builds = context.LibBuilds.Values
                .Where(x => x.CliSuccess == true);

            foreach (LibraryBuildStatus build in builds)
            {
                context.DotNetCorePublish(context.CliDir.FullPath, new DotNetCorePublishSettings
                {
                    Framework = build.CliFramework,
                    Configuration = context.Configuration,
                    OutputDirectory = context.CliPublishDir.Combine(build.CliFramework)
                });
            }
        }

        public override bool ShouldRun(Context context) =>
            context.LibBuilds.Values.Any(x => x.CliSuccess == true);
    }

    [Dependency(typeof(PublishCli))]
    [Dependency(typeof(RestoreTools))]
    public sealed class IlRepackCli : FrostingTask<Context>
    {
        public override void Run(Context context) => context.ILRepack(
            context.CliPublishDir.CombineWithFilePath("VGAudioCli.exe"),
            context.CliPublishDir.CombineWithFilePath("net45/VGAudioCli.exe"),
            new[] { context.CliPublishDir.CombineWithFilePath("net45/VGAudio.dll") });

        public override bool ShouldRun(Context context) =>
            context.LibBuilds["net45"].CliSuccess == true;

        public override void OnError(Exception exception, Context context) =>
            context.Information("Error creating merged assembly.");
    }

    [Dependency(typeof(BuildUwp))]
    public sealed class PublishUwp : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            XDocument manifest = XDocument.Load(context.UwpDir.CombineWithFilePath("Package.appxmanifest").FullPath);
            XNamespace ns = manifest.Root?.GetDefaultNamespace();
            string packageVersion = manifest.Root?.Element(ns + "Identity")?.Attribute("Version").Value;

            string debugSuffix = context.IsReleaseBuild ? "" : "_Debug";
            string packageName = $"VGAudio.Uwp_{packageVersion}_x86_x64_arm{debugSuffix}";
            DirectoryPath packageDir = context.UwpDir.Combine($"AppPackages/VGAudio.Uwp_{packageVersion}{debugSuffix}_Test");

            FilePath appxbundle = packageDir.CombineWithFilePath($"{packageName}.appxbundle");
            var toCopy = new FilePathCollection(new[] { appxbundle }, PathComparer.Default);
            toCopy += packageDir.CombineWithFilePath($"{packageName}.cer");

            if (context.IsReleaseBuild)
            {
                toCopy += packageDir.CombineWithFilePath($"../{packageName}_bundle.appxupload");
            }

            context.EnsureDirectoryExists(context.UwpPublishDir);
            context.CopyFiles(toCopy, context.UwpPublishDir);
        }

        public override bool ShouldRun(Context context) =>
            context.OtherBuilds["uwp"] == true;

        public override void OnError(Exception exception, Context context) =>
            context.Information("Error publishing UWP app.");
    }
}
