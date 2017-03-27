using Cake.Frosting;

namespace Build.Tasks
{
    [Dependency(typeof(CleanBuild))]
    [Dependency(typeof(CleanBin))]
    [Dependency(typeof(Build))]
    [Dependency(typeof(Publish))]
    [Dependency(typeof(IlRepackCli))]
    [Dependency(typeof(TestLibrary))]
    [Dependency(typeof(Sign))]
    [Dependency(typeof(BuildReport))]
    public sealed class Default : FrostingTask<Context> { }

    [Dependency(typeof(Default))]
    [Dependency(typeof(VerifyBuildSuccess))]
    public sealed class AppVeyor : FrostingTask<Context> { }

    [Dependency(typeof(CleanBuild))]
    [Dependency(typeof(CleanTopBin))]
    public sealed class Clean : FrostingTask<Context> { }

    [Dependency(typeof(BuildLibrary))]
    [Dependency(typeof(BuildCli))]
    [Dependency(typeof(BuildUwp))]
    public sealed class Build : FrostingTask<Context> { }

    [Dependency(typeof(BuildLibraryNetStandard))]
    [Dependency(typeof(BuildLibraryNet45))]
    public sealed class BuildLibrary : FrostingTask<Context> { }

    [Dependency(typeof(BuildCliNetCore))]
    [Dependency(typeof(BuildCliNet45))]
    public sealed class BuildCli : FrostingTask<Context> { }
    
    [Dependency(typeof(PublishLibrary))]
    [Dependency(typeof(PublishCli))]
    [Dependency(typeof(PublishUwp))]
    public sealed class Publish : FrostingTask<Context> { }

    [Dependency(typeof(TestLibraryNetStandard))]
    [Dependency(typeof(TestLibraryNet45))]
    public sealed class TestLibrary : FrostingTask<Context> { }

    [Dependency(typeof(SignLibrary))]
    [Dependency(typeof(SignCli))]
    public sealed class Sign : FrostingTask<Context> { }
}
