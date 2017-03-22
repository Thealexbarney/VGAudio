using Cake.Frosting;

namespace Build.Tasks
{
    [Dependency(typeof(Build))]
    [Dependency(typeof(Publish))]
    [Dependency(typeof(TestLibrary))]
    public sealed class Default : FrostingTask<Context> { }
}