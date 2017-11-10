using System;
using System.Collections.Generic;
using Cake.Common;
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
        public DirectoryPath PackageDir { get; set; }

        public DirectoryPath SourceDir { get; set; }
        public DirectoryPath LibraryDir { get; set; }
        public DirectoryPath CliDir { get; set; }
        public DirectoryPath TestsDir { get; set; }
        public DirectoryPath ToolsDir { get; set; }
        public DirectoryPath UwpDir { get; set; }
        public DirectoryPath BenchmarkDir { get; set; }

        public FilePath SolutionFile { get; set; }
        public FilePath LibraryCsproj { get; set; }
        public FilePath CliCsproj { get; set; }
        public FilePath ToolsCsproj { get; set; }
        public FilePath TestsCsproj { get; set; }
        public FilePath UwpCsproj { get; set; }
        public FilePath BenchmarkCsproj { get; set; }

        public DirectoryPath TopBinDir { get; set; }
        public DirectoryPath BinDir { get; set; }
        public DirectoryPath CliBinDir { get; set; }

        public string AppxPublisher { get; set; }

        public string ReleaseCertThumbprint { get; set; }

        public HashSet<string> TestFrameworks { get; } = new HashSet<string>();
        public bool BuildUwp { get; private set; }
        public bool BuildUwpStore { get; private set; }
        public bool RunNetCore { get; private set; }
        public bool RunNetFramework { get; private set; }
        public bool RunBuild { get; private set; }
        public bool RunTests { get; private set; }
        public bool RunClean { get; private set; }
        public bool RunCleanAll { get; private set; }

        public Dictionary<string, LibraryBuildStatus> LibBuilds { get; } = new Dictionary<string, LibraryBuildStatus>
        {
            ["core"] = new LibraryBuildStatus("netstandard1.1", "netcoreapp2.0", "netcoreapp2.0", "netcoreapp2.0"),
            ["full"] = new LibraryBuildStatus("net45", "net451", "net451", "net46")
        };

        private void EnableCore()
        {
            TestFrameworks.Add(LibBuilds["core"].TestFramework);
            RunNetCore = true;
        }

        private void EnableFull()
        {
            TestFrameworks.Add(LibBuilds["full"].TestFramework);
            RunNetFramework = true;
        }

        public void ParseArguments()
        {
            bool core = this.Argument("core", false);
            bool full = this.Argument("full", false);
            bool uwp = this.Argument("uwp", false);
            bool uwpStore = this.Argument("uwpstore", false);
            bool build = this.Argument("build", false);
            bool test = this.Argument("test", false);
            bool clean = this.Argument("clean", false);
            bool cleanAll = this.Argument("cleanAll", false);

            if (core) EnableCore();
            if (full) EnableFull();
            BuildUwp = uwp;
            BuildUwpStore = uwpStore;
            RunBuild = build;
            RunTests = test;
            RunClean = clean;
            RunCleanAll = cleanAll;

            if (!(build || test | clean | cleanAll))
            {
                throw new Exception("Must specify at least one of \"-Build\", \"-Test\", \"-Clean\", \"-CleanAll\"");
            }

            if ((build || test) && !(core || full || uwp || uwpStore))
            {
                throw new Exception("Must specify at least one of \"-NetCore\", \"-NetFramework\", \"-Uwp\", \"-UwpStore\"");
            }
        }

        public void EnableAll()
        {
            EnableCore();
            EnableFull();
            BuildUwp = true;
            BuildUwpStore = true;
            RunBuild = true;
            RunTests = true;
            RunCleanAll = true;
        }
    }
}