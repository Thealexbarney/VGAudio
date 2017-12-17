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

        public DirectoryPath BaseDir { get; set; }
        public DirectoryPath PackageDir { get; set; }
        public DirectoryPath SourceDir { get; set; }
        public DirectoryPath UwpDir { get; set; }
        public DirectoryPath CliPackageDir { get; set; }
        public FilePath BuildTargetsFile { get; set; }
        public FilePath TrimSolution { get; set; }

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
        public bool RunTrim { get; private set; }

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
            RunTrim = this.Argument("trim", false);
            if (this.IsRunningOnUnix()) RunNetCore = true;

            if (!(RunBuild || RunTests || RunClean || RunCleanAll || RunTrim))
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