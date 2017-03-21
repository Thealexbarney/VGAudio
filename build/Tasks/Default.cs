using Cake.Frosting;

namespace Build.Tasks
{
    [Dependency(typeof(BuildLibrary))]
    public sealed class Default : FrostingTask<Context>
    {
    }
}