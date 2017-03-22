using Cake.Frosting;

namespace Build.Tasks
{
    [Dependency(typeof(CleanBuild))]
    [Dependency(typeof(CleanPublish))]
    public sealed class Clean : FrostingTask<Context> { }

    [Dependency(typeof(BuildLibrary))]
    [Dependency(typeof(BuildCli))]
    public sealed class Build : FrostingTask<Context> { }

    [Dependency(typeof(BuildLibraryNetStandard))]
    [Dependency(typeof(BuildLibraryNet45))]
    public sealed class BuildLibrary : FrostingTask<Context> { }

    [Dependency(typeof(BuildCliNetCore))]
    [Dependency(typeof(BuildCliNet45))]
    public sealed class BuildCli : FrostingTask<Context> { }

    [Dependency(typeof(PublishLibrary))]
    [Dependency(typeof(PublishCli))]
    public sealed class Publish : FrostingTask<Context> { }

    [Dependency(typeof(TestLibraryNetStandard))]
    [Dependency(typeof(TestLibraryNet45))]
    public sealed class TestLibrary : FrostingTask<Context> { }
}
