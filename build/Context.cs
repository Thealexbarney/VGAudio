using System;
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
        public FilePath BuildTargetsFile { get; set; }
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

        public bool BuildUwp { get; private set; }
        public bool BuildUwpStore { get; private set; }
        public bool RunNetCore { get; private set; }
        public bool RunNetFramework { get; private set; }
        public bool RunBuild { get; private set; }
        public bool RunTests { get; private set; }
        public bool RunClean { get; private set; }
        public bool RunCleanAll { get; private set; }

        public LibraryBuildStatus FullBuilds { get; } = new LibraryBuildStatus("net45", "net451", "net451", "net46");

        public void ParseArguments()
        {
            RunNetCore = this.Argument("core", false);
            RunNetFramework = this.Argument("full", false);
            BuildUwp = this.Argument("uwp", false);
            BuildUwpStore = this.Argument("uwpstore", false);
            RunBuild = this.Argument("build", false);
            RunTests = this.Argument("test", false);
            RunClean = this.Argument("clean", false);
            RunCleanAll = this.Argument("cleanAll", false);
            if (this.IsRunningOnUnix()) RunNetCore = true;

            if (!(RunBuild || RunTests | RunClean | RunCleanAll))
            {
                if (this.IsRunningOnUnix())
                    throw new Exception("Must specify at least one of \"--Build\", \"--Test\", \"--Clean\", \"--CleanAll\"");

                throw new Exception("Must specify at least one of \"-Build\", \"-Test\", \"-Clean\", \"-CleanAll\"");
            }

            if ((RunBuild || RunTests) && !(RunNetCore || RunNetFramework || BuildUwp || BuildUwpStore))
            {
                throw new Exception("Must specify at least one of \"-NetCore\", \"-NetFramework\", \"-Uwp\", \"-UwpStore\"");
            }
        }

        public void EnableAll()
        {
            RunNetCore = true;
            RunNetFramework = true;
            BuildUwp = true;
            BuildUwpStore = true;
            RunBuild = true;
            RunTests = true;
            RunCleanAll = true;
        }
    }
}