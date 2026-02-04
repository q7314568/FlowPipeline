using FlowPipeline.Abstractions;
using FlowPipeline.Core;
using FlowPipeline.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FlowPipeline.Tests;

public class PipelineBuilderTests
{
    [Fact]
    public async Task BasicPipeline_ShouldExecuteSuccessfully()
    {
        // Arrange & Act
        var result = await PipelineBuilder<int>
            .Start(null, 5)
            .Then(async (value, ct) => FlowResult<int>.Success(value * 2))
            .Then(async (value, ct) => FlowResult<int>.Success(value + 10))
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(20, result.Value);
    }

    [Fact]
    public async Task Pipeline_WithFailure_ShouldShortCircuit()
    {
        // Arrange
        var executedThirdStep = false;

        // Act
        var result = await PipelineBuilder<int>
            .Start(null, 5)
            .Then(async (value, ct) => FlowResult<int>.Success(value * 2))
            .Then(async (value, ct) => FlowResult<int>.Fail("Second step failed", "ERROR_CODE"))
            .Then(async (value, ct) =>
            {
                executedThirdStep = true;
                return FlowResult<int>.Success(value + 10);
            })
            .ExecuteAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Second step failed", result.ErrorMessage);
        Assert.Equal("ERROR_CODE", result.ErrorCode);
        Assert.False(executedThirdStep, "Third step should not execute after failure");
    }

    [Fact]
    public async Task Pipeline_WithException_ShouldWrapAsFailure()
    {
        // Act
        var result = await PipelineBuilder<int>
            .Start(null, 5)
            .Then<int>(async (value, ct) => throw new InvalidOperationException("Test exception"))
            .ExecuteAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Test exception", result.ErrorMessage);
        Assert.Equal("STEP_EXCEPTION", result.ErrorCode);
    }

    [Fact]
    public async Task UnitPipeline_ShouldWork()
    {
        // Act
        var result = await PipelineBuilder<Unit>
            .Start(null)
            .Then(async (unit, ct) => FlowResult<int>.Success(42))
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task ConditionalStep_WhenConditionMet_ShouldExecute()
    {
        // Act
        var result = await PipelineBuilder<int>
            .Start(null, 15)
            .ThenWhen(
                value => value > 10,
                async (value, ct) => FlowResult<string>.Success($"Large: {value}")
            )
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Large: 15", result.Value);
    }

    [Fact]
    public async Task ConditionalStep_WhenConditionNotMet_ShouldFail()
    {
        // Act
        var result = await PipelineBuilder<int>
            .Start(null, 5)
            .ThenWhen(
                value => value > 10,
                async (value, ct) => FlowResult<string>.Success($"Large: {value}")
            )
            .ExecuteAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Condition not met", result.ErrorMessage);
    }

    [Fact]
    public async Task SideEffect_ShouldNotChangeValue()
    {
        // Arrange
        var sideEffectValue = 0;

        // Act
        var result = await PipelineBuilder<int>
            .Start(null, 10)
            .ThenDo(async (value, ct) => { sideEffectValue = value; })
            .Then(async (value, ct) => FlowResult<int>.Success(value * 2))
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(20, result.Value);
        Assert.Equal(10, sideEffectValue);
    }

    [Fact]
    public async Task ThenRun_ShouldNotChangeValue()
    {
        // Arrange
        var runCalled = false;

        // Act
        var result = await PipelineBuilder<int>
            .Start(null, 10)
            .ThenRun(async ct => { runCalled = true; })
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value);
        Assert.True(runCalled);
    }

    [Fact]
    public async Task MapExtension_ShouldTransformValue()
    {
        // Act
        var result = await PipelineBuilder<int>
            .Start(null, 10)
            .Map(x => x * 2)
            .Map(x => x + 5)
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(25, result.Value);
    }

    [Fact]
    public async Task DependencyInjection_ShouldResolveSteps()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<MultiplyStep>();
        services.AddTransient<AddStep>();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var result = await PipelineBuilder<int>
            .Start(serviceProvider, 5)
            .Then<MultiplyStep, int>()
            .Then<AddStep, int>()
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(20, result.Value);
    }

    [Fact]
    public async Task DependencyInjection_WithoutProvider_ShouldFail()
    {
        // Act
        var result = await PipelineBuilder<int>
            .Start(null, 5)
            .Then<MultiplyStep, int>()
            .ExecuteAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Cannot resolve", result.ErrorMessage);
    }

    [Fact]
    public void FlowResult_Fail_WithCustomError_ShouldStoreErrorPayload()
    {
        // Arrange
        var customError = new TestError
        {
            Message = "Test error",
            Code = "TEST_CODE",
            TestProperty = "TestValue"
        };

        // Act
        var result = FlowResult<int>.Fail("Test error", customError, "TEST_CODE");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Test error", result.ErrorMessage);
        Assert.Equal("TEST_CODE", result.ErrorCode);
        Assert.NotNull(result.ErrorPayload);
        Assert.IsType<TestError>(result.ErrorPayload);
    }

    [Fact]
    public void FlowResultExtensions_TryGetError_ShouldExtractTypedError()
    {
        // Arrange
        var customError = new TestError
        {
            Message = "Test error",
            Code = "TEST_CODE",
            TestProperty = "TestValue"
        };
        var result = FlowResult<int>.Fail("Test error", customError, "TEST_CODE");

        // Act
        var success = result.TryGetError<int, TestError>(out var error);

        // Assert
        Assert.True(success);
        Assert.NotNull(error);
        Assert.Equal("TestValue", error.TestProperty);
    }

    [Fact]
    public void FlowResultExtensions_GetErrorAs_ShouldReturnTypedError()
    {
        // Arrange
        var customError = new TestError
        {
            Message = "Test error",
            Code = "TEST_CODE",
            TestProperty = "TestValue"
        };
        var result = FlowResult<int>.Fail("Test error", customError, "TEST_CODE");

        // Act
        var error = result.GetErrorAs<int, TestError>();

        // Assert
        Assert.NotNull(error);
        Assert.Equal("TestValue", error.TestProperty);
    }

    [Fact]
    public async Task StepInstance_ShouldExecuteCorrectly()
    {
        // Arrange
        var step = new MultiplyStep();

        // Act
        var result = await PipelineBuilder<int>
            .Start(null, 5)
            .Then(step)
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value);
    }

    [Fact]
    public async Task ActionInstance_ShouldExecuteCorrectly()
    {
        // Arrange
        var actionCalled = false;
        var action = new TestAction(() => actionCalled = true);

        // Act
        var result = await PipelineBuilder<int>
            .Start(null, 5)
            .ThenDo(action)
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value);
        Assert.True(actionCalled);
    }
}

// Test implementations
public class MultiplyStep : IPipelineStep<int, int>
{
    public Task<FlowResult<int>> ProcessAsync(int input, CancellationToken ct = default)
    {
        return Task.FromResult(FlowResult<int>.Success(input * 2));
    }
}

public class AddStep : IPipelineStep<int, int>
{
    public Task<FlowResult<int>> ProcessAsync(int input, CancellationToken ct = default)
    {
        return Task.FromResult(FlowResult<int>.Success(input + 10));
    }
}

public class TestError : PipelineError
{
    public string TestProperty { get; init; } = string.Empty;
}

public class TestAction : IPipelineAction<int>
{
    private readonly Action _action;

    public TestAction(Action action)
    {
        _action = action;
    }

    public Task ExecuteAsync(int input, CancellationToken ct = default)
    {
        _action();
        return Task.CompletedTask;
    }
}
