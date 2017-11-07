using System.Linq;
using System.Xml.Linq;
using Cake.Common;
using Cake.Common.IO;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.MSBuild;
using Cake.Common.Tools.MSBuild;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Frosting;
using ILRepacking;
using static Build.Utilities;

namespace Build.Tasks
{
    public sealed class RunNonUwpBuild : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            BuildTasks.BuildNonUwp(context);

            if (context.RunNetFramework)
            {
                BuildTasks.IlMergeCli(context);
            }

            BuildTasks.PackageNonUwp(context);
        }

        public override bool ShouldRun(Context context) =>
           context.RunBuild && context.LibraryFrameworks.Any() && context.CliFrameworks.Any();
    }

    public sealed class RestoreUwp : FrostingTask<Context>
    {
        public override void Run(Context context) =>
            context.MSBuild(context.UwpDir.CombineWithFilePath("VGAudio.Uwp.csproj"), new MSBuildSettings
            {
                Targets = { "Restore" },
                Verbosity = Verbosity.Minimal
            });

        public override bool ShouldRun(Context context) => context.IsRunningOnWindows() && context.BuildUwp;
    }

    [Dependency(typeof(RestoreUwp))]
    public sealed class RunUwpBuild : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            BuildTasks.BuildUwp(context);
            BuildTasks.PackageUwp(context);
        }

        public override bool ShouldRun(Context context) => context.IsRunningOnWindows() && context.BuildUwp;
        public override void Finally(Context context) => DeleteFile(context, context.UwpSideloadManifest, false);
    }

    public static class BuildTasks
    {
        public static void BuildNonUwp(Context context)
        {
            string[] cliFrameworks = context.CliFrameworks.ToArray();
            string[] libFrameworks = context.LibraryFrameworks.ToArray();
            string[] testFrameworks = context.TestFrameworks.ToArray();

            var settings = new DotNetCoreMSBuildSettings
            {
                Targets = { "BuildNonUwp" },
                ArgumentCustomization = args =>
                {
                    args.Append($"/p:LibraryFrameworks=\\\"{string.Join(";", libFrameworks)}\\\"");
                    args.Append($"/p:CliFrameworks=\\\"{string.Join(";", cliFrameworks)}\\\"");
                    args.Append($"/p:TestFrameworks=\\\"{string.Join(";", testFrameworks)}\\\"");
                    return args;
                }
            };

            settings.Properties.Add("LibraryProjectFile", new[] { context.LibraryCsproj.FullPath });
            settings.Properties.Add("CliProjectFile", new[] { context.CliCsproj.FullPath });
            settings.Properties.Add("ToolsProjectFile", new[] { context.ToolsCsproj.FullPath });

            settings.Properties.Add("CliOutputPath", new[] { context.CliBinDir.FullPath });
            settings.Properties.Add("LibraryOutputPath", new[] { context.LibraryBinDir.FullPath });

            settings.Properties.Add("IncludeSymbols", new[] { "true" });
            settings.Properties.Add("IncludeSource", new[] { "true" });
            settings.Properties.Add("Configuration", new[] { context.Configuration });
            context.DotNetCoreMSBuild(context.LibraryCsproj.FullPath, settings);

            DeleteFile(context, context.CliBinDir.Combine(context.LibBuilds["full"].ToolsFramework).CombineWithFilePath("VGAudioTools.runtimeconfig.json"), false);
            DeleteFile(context,
                context.CliBinDir.Combine(context.LibBuilds["full"].CliFramework)
                    .CombineWithFilePath("VGAudioCli.runtimeconfig.json"), false);
        }


        public static void BuildUwp(Context context)
        {
            SetupUwpSigningCertificate(context);

            var settings = new MSBuildSettings
            {
                Verbosity = Verbosity.Minimal,
                MSBuildPlatform = MSBuildPlatform.x86,
                Configuration = context.Configuration
            };

            settings.WithProperty("VisualStudioVersion", "15.0");

            context.MSBuild(context.UwpCsproj, settings.WithProperty("AppxBuildType", "Store"));

            //The second manifest MUST be written after the first build, otherwise incremental builds will mess stuff up
            CreateSideloadAppxmanifest(context);
            context.MSBuild(context.UwpCsproj, settings.WithProperty("AppxBuildType", "Sideload"));
        }

        public static void PackageNonUwp(Context context)
        {
            context.CopyDirectory(context.LibraryBinDir, context.PackageDir);

            context.EnsureDirectoryExists(context.PackageDir);
            context.Zip(context.CliBinDir, context.PackageDir.CombineWithFilePath("VGAudioCli.zip"));
            context.CopyFiles(context.GetFiles($"{context.CliBinDir}/*.exe"), context.PackageDir);
        }

        public static void IlMergeCli(Context context)
        {
            string cliPath = context.CliBinDir.CombineWithFilePath($"{context.LibBuilds["full"].CliFramework}/VGAudioCli.exe").FullPath;
            string libPath = context.CliBinDir.CombineWithFilePath($"{context.LibBuilds["full"].CliFramework}/VGAudio.dll").FullPath;
            string toolsPath = context.CliBinDir.CombineWithFilePath($"{context.LibBuilds["full"].ToolsFramework}/VGAudioTools.exe").FullPath;

            var cliOptions = new RepackOptions
            {
                OutputFile = context.CliBinDir.CombineWithFilePath("VGAudioCli.exe").FullPath,
                InputAssemblies = new[] { cliPath, libPath },
                SearchDirectories = new[] { "." }
            };

            var toolsOptions = new RepackOptions
            {
                OutputFile = context.CliBinDir.CombineWithFilePath("VGAudioTools.exe").FullPath,
                InputAssemblies = new[] { toolsPath, libPath },
                SearchDirectories = new[] { "." }
            };

            new ILRepack(cliOptions).Repack();
            new ILRepack(toolsOptions).Repack();
        }

        public static void PackageUwp(Context context)
        {
            XDocument manifest = XDocument.Load(context.UwpDir.CombineWithFilePath("Package.appxmanifest").FullPath);
            XNamespace ns = manifest.Root?.GetDefaultNamespace();
            string packageVersion = manifest.Root?.Element(ns + "Identity")?.Attribute("Version")?.Value;

            string debugSuffix = context.IsReleaseBuild ? "" : "_Debug";
            string packageName = $"VGAudio_{packageVersion}_x86_x64_arm{debugSuffix}";
            DirectoryPath packageDir = context.UwpDir.Combine($"AppPackages/VGAudio_{packageVersion}{debugSuffix}_Test");

            FilePath appxbundle = packageDir.CombineWithFilePath($"{packageName}.appxbundle");
            var toCopy = new FilePathCollection(new[] { appxbundle }, PathComparer.Default);
            toCopy += packageDir.CombineWithFilePath($"{packageName}.cer");

            if (context.IsReleaseBuild)
            {
                toCopy += packageDir.CombineWithFilePath($"../{packageName}_bundle.appxupload");
            }

            context.EnsureDirectoryExists(context.UwpBinDir);
            context.CopyFiles(toCopy, context.UwpBinDir);

            context.EnsureDirectoryExists(context.PackageDir);
            context.CopyFiles(context.GetFiles($"{context.UwpBinDir}/*.appxbundle"), context.PackageDir);
        }
    }
}
