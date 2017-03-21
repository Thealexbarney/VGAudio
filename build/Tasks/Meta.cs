using Cake.Frosting;

namespace Build.Tasks
{
    [Dependency(typeof(TestLibraryNetStandard))]
    [Dependency(typeof(TestLibraryNet45))]
    public sealed class TestLibrary : FrostingTask<Context> { }

    [Dependency(typeof(BuildLibraryNetStandard))]
    [Dependency(typeof(BuildLibraryNet45))]
    public sealed class BuildLibrary : FrostingTask<Context> { }

    [Dependency(typeof(BuildCliNetCore))]
    [Dependency(typeof(BuildCliNet45))]
    public sealed class BuildCli : FrostingTask<Context> { }
}
