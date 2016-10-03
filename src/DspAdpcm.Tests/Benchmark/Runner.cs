using BenchmarkDotNet.Running;

namespace DspAdpcm.Tests.Benchmark
{
    public static class Runner
    {
        public static int Main(string[] args)
        {
            BenchmarkRunner.Run<BrstmBenchmarks>();
            return 0;
        }
    }
}
