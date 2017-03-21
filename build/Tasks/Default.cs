using Cake.Frosting;

namespace Build.Tasks
{
    [Dependency(typeof(BuildLibrary))]
    [Dependency(typeof(BuildCli))]
    [Dependency(typeof(TestLibrary))]
    public sealed class Default : FrostingTask<Context>
    {
    }
}