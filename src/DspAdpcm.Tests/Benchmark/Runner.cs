using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace DspAdpcm.Tests.Benchmark
{
    public static class Runner
    {
        public static int Main(string[] args)
        {
            BenchmarkRunner.Run<BrstmBenchmarks>();
            //BenchmarkRunner.Run<BrstmBenchmarks>(
            //    ManualConfig
            //.Create(DefaultConfig.Instance)
            //.With(new MemoryDiagnoser())
            //.With(ExecutionValidator.FailOnError));
            return 0;
        }
    }
}
