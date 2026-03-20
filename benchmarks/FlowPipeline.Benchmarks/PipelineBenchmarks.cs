using BenchmarkDotNet.Attributes;
using FlowPipeline.Abstractions;
using FlowPipeline.Core;

namespace FlowPipeline.Benchmarks;

[MemoryDiagnoser]
public class PipelineBenchmarks
{
    private readonly NoopObserver _observer = new();

    [Benchmark]
    public async Task<FlowResult<int>> SuccessfulPipeline()
    {
        return await PipelineBuilder
            .Start(null, 10)
            .Then(async (value, ct) => FlowResult<int>.Success(value * 2))
            .Then(async (value, ct) => FlowResult<int>.Success(value + 5))
            .ExecuteAsync();
    }

    [Benchmark]
    public async Task<FlowResult<int>> FailedPipelineShortCircuit()
    {
        return await PipelineBuilder
            .Start(null, 10)
            .Then(async (value, ct) => FlowResult<int>.Fail("Failed", "FAILED"))
            .Then(async (value, ct) => FlowResult<int>.Success(value + 1))
            .ExecuteAsync();
    }

    [Benchmark]
    public async Task<FlowResult<int>> ObservedPipeline()
    {
        return await PipelineBuilder
            .Start(
                null,
                10,
                new PipelineOptions
                {
                    Observers = new[] { _observer }
                })
            .Then(async (value, ct) => FlowResult<int>.Success(value * 2))
            .ThenDo(async (value, ct) => await Task.CompletedTask)
            .Then(async (value, ct) => FlowResult<int>.Success(value + 5))
            .ExecuteAsync();
    }

    private sealed class NoopObserver : IPipelineObserver
    {
    }
}
