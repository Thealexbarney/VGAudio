using System;
using System.Collections.Generic;
using System.Linq;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build
{
    public class Context : FrostingContext
    {
        public Context(ICakeContext context) : base(context) { }

        public string Configuration { get; set; }
        public bool IsReleaseBuild => Configuration.Equals("release", StringComparison.CurrentCultureIgnoreCase);

        public DirectoryPath BaseDir { get; set; }
        public DirectoryPath BuildDir { get; set; }
        public DirectoryPath CakeToolsDir { get; set; }
        public DirectoryPath PackageDir { get; set; }

        public DirectoryPath SourceDir { get; set; }
        public DirectoryPath LibraryDir { get; set; }
        public DirectoryPath CliDir { get; set; }
        public DirectoryPath TestsDir { get; set; }
        public DirectoryPath ToolsDir { get; set; }
        public DirectoryPath UwpDir { get; set; }

        public FilePath TestsCsproj { get; set; }
        public FilePath UwpCsproj { get; set; }

        public DirectoryPath TopBinDir { get; set; }
        public DirectoryPath BinDir { get; set; }
        public DirectoryPath LibraryBinDir { get; set; }
        public DirectoryPath CliBinDir { get; set; }
        public DirectoryPath UwpBinDir { get; set; }

        public FilePath UwpStoreManifest { get; set; }
        public FilePath UwpSideloadManifest { get; set; }
        public string SideloadAppxName { get; set; }
        public string AppxPublisher { get; set; }

        public string ReleaseCertThumbprint { get; set; }

        public Dictionary<string, LibraryBuildStatus> LibBuilds { get; } = new Dictionary<string, LibraryBuildStatus>
        {
            ["netstandard"] = new LibraryBuildStatus("netstandard1.1", "netcoreapp1.0", "netcoreapp1.0", "netcoreapp1.0"),
            ["net45"] = new LibraryBuildStatus("net45", "net45", "net45", "net46")
        };

        public Dictionary<string, bool?> OtherBuilds { get; } = new Dictionary<string, bool?>
        {
            ["uwp"] = null
        };

        public bool LibraryBuildsSucceeded => LibBuilds.Values.All(x => x.LibSuccess == true);
        public bool CliBuildsSucceeded => LibBuilds.Values.All(x => x.CliSuccess == true);
        public bool ToolsBuildsSucceeded => LibBuilds.Values.All(x => x.ToolsSuccess == true);
        public bool TestsSucceeded => LibBuilds.Values.All(x => x.TestSuccess == true);
    }
}