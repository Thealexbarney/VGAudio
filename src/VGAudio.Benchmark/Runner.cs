using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using VGAudio.Benchmark.AdpcmBenchmarks;
using VGAudio.Benchmark.HelperBenchmarks;

namespace VGAudio.Benchmark
{
    public static class Runner
    {
        private static Type[] Benchmarks { get; } =
        {
            typeof(BuildParseBenchmarks),
            typeof(EncodeBenchmarks),
            typeof(DecodeBenchmarks),
            typeof(InterleaveBenchmarks),
            typeof(BitReaderBenchmarks)
        };

        public static IConfig FastConfig { get; } =
            ManualConfig
            .Create(DefaultConfig.Instance)
            .With(Job.RyuJitX64
                .WithLaunchCount(1)
                .WithIterationTime(TimeInterval.Millisecond * 100)
                .WithWarmupCount(3)
                .WithTargetCount(3)
            );

        public static void Main(string[] args)
        {
            var switcher = new BenchmarkSwitcher(Benchmarks);
            switcher.Run(args);
            //switcher.Run(args, FastConfig);
        }
    }
}
