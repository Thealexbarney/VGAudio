using Cake.Frosting;

namespace Build.Tasks
{
    [Dependency(typeof(Rebuild))]
    [Dependency(typeof(Test))]
    [Dependency(typeof(Package))]
    [Dependency(typeof(BuildReport))]
    public sealed class Default : FrostingTask<Context> { }

    [Dependency(typeof(CleanBuild))]
    [Dependency(typeof(CleanTopBin))]
    [Dependency(typeof(CleanPackage))]
    public sealed class Clean : FrostingTask<Context> { }

    [Dependency(typeof(CleanBuild))]
    [Dependency(typeof(CleanBin))]
    [Dependency(typeof(Build))]
    public sealed class Rebuild : FrostingTask<Context> { }

    [Dependency(typeof(BuildLibrary))]
    [Dependency(typeof(BuildCli))]
    [Dependency(typeof(BuildTools))]
    [Dependency(typeof(BuildUwp))]
    [Dependency(typeof(Publish))]
    public sealed class Build : FrostingTask<Context> { }

    [Dependency(typeof(PublishLibrary))]
    [Dependency(typeof(PublishCli))]
    [Dependency(typeof(PublishTools))]
    [Dependency(typeof(IlRepackCli))]
    [Dependency(typeof(IlRepackTools))]
    [Dependency(typeof(PublishUwp))]
    public sealed class Publish : FrostingTask<Context> { }

    [Dependency(typeof(TestLibraryNetStandard))]
    [Dependency(typeof(TestLibraryNet45))]
    public sealed class Test : FrostingTask<Context> { }

    [Dependency(typeof(SignLibrary))]
    [Dependency(typeof(SignCli))]
    [Dependency(typeof(SignUwp))]
    public sealed class Sign : FrostingTask<Context> { }

    [Dependency(typeof(Publish))]
    [Dependency(typeof(CleanPackage))]
    [Dependency(typeof(PackageLibrary))]
    [Dependency(typeof(PackageCli))]
    [Dependency(typeof(PackageUwp))]
    public sealed class Package : FrostingTask<Context> { }

    [Dependency(typeof(Clean))]
    [Dependency(typeof(Publish))]
    [Dependency(typeof(Sign))]
    [Dependency(typeof(Package))]
    public sealed class Release : FrostingTask<Context> { }

    [Dependency(typeof(Default))]
    [Dependency(typeof(UploadAppVeyorArtifacts))]
    [Dependency(typeof(VerifyBuildSuccess))]
    public sealed class AppVeyor : FrostingTask<Context> { }
}
