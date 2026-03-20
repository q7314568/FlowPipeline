using FlowPipeline.Abstractions;
using FlowPipeline.Core;
using FlowPipeline.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FlowPipeline.Tests;

public class PipelineBuilderTests
{
    [Fact]
    public async Task StaticPipelineBuilder_ShouldExecuteSuccessfully()
    {
        var result = await PipelineBuilder
            .Start(null, 5)
            .Then(async (value, ct) => FlowResult<int>.Success(value * 2))
            .Then(async (value, ct) => FlowResult<int>.Success(value + 10))
            .ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(20, result.Value);
    }

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
        Assert.NotNull(result.Exception);
        Assert.IsType<InvalidOperationException>(result.Exception);
        Assert.Equal("Test exception", result.Exception!.Message);
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
    public async Task StaticPipelineBuilder_UnitStart_ShouldWork()
    {
        var result = await PipelineBuilder
            .Start(null)
            .Then(async (unit, ct) => FlowResult<int>.Success(42))
            .ExecuteAsync();

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
                async (value, ct) => FlowResult<int>.Success(value * 2)
            )
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(30, result.Value);
    }

    [Fact]
    public async Task ConditionalStep_WhenConditionNotMet_ShouldSkipAndContinue()
    {
        var conditionalExecuted = false;

        // Act
        var result = await PipelineBuilder<int>
            .Start(null, 5)
            .ThenWhen(
                value => value > 10,
                async (value, ct) =>
                {
                    conditionalExecuted = true;
                    return FlowResult<int>.Success(value * 2);
                }
            )
            .Then(async (value, ct) => FlowResult<int>.Success(value + 3))
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(conditionalExecuted);
        Assert.Equal(8, result.Value);
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
        Assert.Null(result.Exception);
        Assert.NotNull(result.Failure);
        Assert.Equal("Test error", result.Failure!.Message);
        Assert.Equal("TEST_CODE", result.Failure.Code);
    }

    [Fact]
    public void FlowResult_Fail_WithException_ShouldStoreOriginalException()
    {
        // Arrange
        var exception = new InvalidOperationException("Boom");

        // Act
        var result = FlowResult<int>.FailFromException("Step failed", exception, "STEP_EXCEPTION");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("STEP_EXCEPTION", result.ErrorCode);
        Assert.Same(exception, result.Exception);
        Assert.NotNull(result.Failure);
        Assert.Same(exception, result.Failure!.Exception);
    }

    [Fact]
    public void FlowResult_Fail_WithStructuredFailure_ShouldPreserveAllFailureDetails()
    {
        // Arrange
        var inner = new InvalidOperationException("Inner");
        var exception = new ApplicationException("Outer", inner);
        var payload = new TestError
        {
            Message = "Structured",
            Code = "STRUCTURED",
            TestProperty = "Payload"
        };
        var failure = new FlowFailure("Structured failure", "STRUCTURED_ERROR", payload, exception);

        // Act
        var result = FlowResult<int>.Fail(failure);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Same(failure, result.Failure);
        Assert.Equal("Structured failure", result.ErrorMessage);
        Assert.Equal("STRUCTURED_ERROR", result.ErrorCode);
        Assert.Same(payload, result.ErrorPayload);
        Assert.Same(exception, result.Exception);
    }

    [Fact]
    public void FlowResult_Fail_WithPayloadAndException_ShouldStoreCombinedDiagnostics()
    {
        // Arrange
        var exception = new InvalidOperationException("Payload exception");
        var payload = new TestError
        {
            Message = "Combined",
            Code = "COMBINED",
            TestProperty = "PayloadAndException"
        };

        // Act
        var result = FlowResult<int>.Fail("Combined failure", payload, "COMBINED_ERROR", exception);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Failure);
        Assert.Equal("Combined failure", result.Failure!.Message);
        Assert.Equal("COMBINED_ERROR", result.Failure.Code);
        Assert.Same(payload, result.Failure.Payload);
        Assert.Same(exception, result.Failure.Exception);
    }

    [Fact]
    public void FlowResult_Fail_WhenMessageIsWhitespace_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => FlowResult<int>.Fail(" "));
    }

    [Fact]
    public void FlowResult_Fail_WithNullFailure_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => FlowResult<int>.Fail((FlowFailure)null!));
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
    public void FlowResult_CanStoreEnumPayload()
    {
        // Act
        var result = FlowResult<int>.Fail("Enum error", TestErrorCode.ValidationFailed, "VALIDATION_FAILED");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.TryGetError<int, TestErrorCode>(out var errorCode));
        Assert.Equal(TestErrorCode.ValidationFailed, errorCode);
        Assert.Equal(TestErrorCode.ValidationFailed, result.GetErrorAs<int, TestErrorCode>());
    }

    [Fact]
    public async Task FailurePayload_ShouldBePreservedAcrossShortCircuit()
    {
        // Arrange
        var customError = new TestError
        {
            Message = "Validation failed",
            Code = "VALIDATION_ERROR",
            TestProperty = "PayloadValue"
        };

        // Act
        var result = await PipelineBuilder<int>
            .Start(null, 10)
            .Then(async (value, ct) => FlowResult<int>.Fail("Validation failed", customError, "VALIDATION_ERROR"))
            .Then(async (value, ct) => FlowResult<string>.Success($"Value: {value}"))
            .ExecuteAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Validation failed", result.ErrorMessage);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.True(result.TryGetError<string, TestError>(out var error));
        Assert.Equal("PayloadValue", error!.TestProperty);
    }

    [Fact]
    public async Task FailureException_ShouldBePreservedAcrossShortCircuit()
    {
        // Arrange
        var exception = new InvalidOperationException("Validation blew up");

        // Act
        var result = await PipelineBuilder<int>
            .Start(null, 10)
            .Then((value, ct) => Task.FromResult(FlowResult<int>.FailFromException("Validation failed", exception, "VALIDATION_ERROR")))
            .Then(async (value, ct) => FlowResult<string>.Success($"Value: {value}"))
            .ExecuteAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Same(exception, result.Exception);
        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task Pipeline_Observers_ShouldReceiveExecutionAndStageMetadata()
    {
        // Arrange
        var observer = new TestObserver();
        var options = new PipelineOptions
        {
            Name = "ObservedPipeline",
            Observers = new[] { observer }
        };

        // Act
        var result = await PipelineBuilder
            .Start(null, 10, options)
            .Then(async (value, ct) => FlowResult<int>.Success(value + 5))
            .ThenDo(async (value, ct) => await Task.CompletedTask)
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(observer.ExecutionStarted);
        Assert.Single(observer.ExecutionCompleted);
        Assert.Equal(2, observer.StageStarted.Count);
        Assert.Equal(2, observer.StageCompleted.Count);
        Assert.All(observer.StageStarted, context => Assert.Equal("ObservedPipeline", context.Execution.PipelineName));
        Assert.Collection(
            observer.StageStarted.OrderBy(x => x.StageIndex),
            first =>
            {
                Assert.Equal(1, first.StageIndex);
                Assert.Equal(PipelineStageKind.Step, first.StageKind);
                Assert.Equal(typeof(int), first.InputType);
                Assert.Equal(typeof(int), first.OutputType);
                Assert.Equal(1, first.Attempt);
            },
            second =>
            {
                Assert.Equal(2, second.StageIndex);
                Assert.Equal(PipelineStageKind.Action, second.StageKind);
                Assert.Equal(typeof(int), second.InputType);
                Assert.Equal(typeof(int), second.OutputType);
                Assert.Equal(1, second.Attempt);
            });
        Assert.Equal(observer.ExecutionStarted[0].ExecutionId, observer.ExecutionCompleted[0].ExecutionId);
    }

    [Fact]
    public async Task Pipeline_RetryPolicy_ShouldRetryOnExceptionAndEventuallySucceed()
    {
        // Arrange
        var attempts = 0;
        var options = new PipelineOptions
        {
            Retry = new PipelineRetryOptions
            {
                MaxAttempts = 3,
                ShouldRetryException = ex => ex is InvalidOperationException
            }
        };

        // Act
        var result = await PipelineBuilder
            .Start(null, 1, options)
            .Then<int>(async (value, ct) =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw new InvalidOperationException($"Attempt {attempts}");
                }

                return FlowResult<int>.Success(value + attempts);
            })
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task Pipeline_RetryPolicy_ShouldRetryOnFlowFailureAndEventuallySucceed()
    {
        // Arrange
        var attempts = 0;
        var options = new PipelineOptions
        {
            Retry = new PipelineRetryOptions
            {
                MaxAttempts = 3,
                ShouldRetryFailure = failure => failure.Code == "TRANSIENT"
            }
        };

        // Act
        var result = await PipelineBuilder
            .Start(null, 5, options)
            .Then(async (value, ct) =>
            {
                attempts++;
                return attempts < 3
                    ? FlowResult<int>.Fail("Temporary issue", "TRANSIENT")
                    : FlowResult<int>.Success(value * 2);
            })
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task Pipeline_StageTimeout_ShouldReturnTimeoutFailure()
    {
        // Arrange
        var options = new PipelineOptions
        {
            StageTimeout = TimeSpan.FromMilliseconds(20)
        };

        // Act
        var result = await PipelineBuilder
            .Start(null, 5, options)
            .Then(async (value, ct) =>
            {
                await Task.Delay(100, ct);
                return FlowResult<int>.Success(value);
            })
            .ExecuteAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("STAGE_TIMEOUT", result.ErrorCode);
        Assert.NotNull(result.Exception);
        Assert.IsType<TimeoutException>(result.Exception);
    }

    [Fact]
    public async Task Pipeline_FailureMapper_ShouldCustomizeMappedFailure()
    {
        // Arrange
        var options = new PipelineOptions
        {
            FailureMapper = context => new FlowFailure(
                $"{context.Stage.StageName}:{context.Exception.Message}",
                $"MAPPED_{context.DefaultCode}",
                exception: context.Exception)
        };

        // Act
        var result = await PipelineBuilder
            .Start(null, 5, options)
            .Then<int>(async (value, ct) => throw new InvalidOperationException("Boom"))
            .ExecuteAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("MAPPED_STEP_EXCEPTION", result.ErrorCode);
        Assert.Contains("Boom", result.ErrorMessage);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public async Task Pipeline_WhenObserverThrows_ShouldNotBreakExecution()
    {
        // Arrange
        var options = new PipelineOptions
        {
            Observers = new IPipelineObserver[] { new ThrowingObserver() }
        };

        // Act
        var result = await PipelineBuilder
            .Start(null, 5, options)
            .Then(async (value, ct) => FlowResult<int>.Success(value + 1))
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(6, result.Value);
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
    public async Task ExecuteAsync_WhenStepIsCancelled_ShouldPropagateCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            PipelineBuilder<int>
                .Start(null, 5)
                .Then(async (value, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    return FlowResult<int>.Success(value);
                })
                .ExecuteAsync(cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_WhenActionIsCancelled_ShouldPropagateCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            PipelineBuilder<int>
                .Start(null, 5)
                .ThenDo(async (value, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.CompletedTask;
                })
                .ExecuteAsync(cts.Token));
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

    [Fact]
    public async Task ThenWithParam_WithStepInstance_ShouldPassParameterToStep()
    {
        // Arrange
        var multiplyStep = new MultiplyByStep();

        // Act
        var result = await PipelineBuilder<int>
            .Start(null, 10)
            .ThenWithParam(multiplyStep, 5)
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(50, result.Value);
    }

    [Fact]
    public async Task ThenWithParam_WithLambda_ShouldPassParameterToFunction()
    {
        // Arrange & Act
        var result = await PipelineBuilder<int>
            .Start(null, 10)
            .ThenWithParam(async (value, multiplier, ct) =>
                FlowResult<int>.Success(value * multiplier), 7)
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(70, result.Value);
    }

    [Fact]
    public async Task ThenWithParam_WhenPreviousStepFails_ShouldShortCircuit()
    {
        // Arrange
        var multiplyStep = new MultiplyByStep();

        // Act
        var result = await PipelineBuilder<int>
            .Start(null, 10)
            .Then(async (value, ct) => FlowResult<int>.Fail("Previous step failed", "ERROR"))
            .ThenWithParam(multiplyStep, 5)
            .ExecuteAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Previous step failed", result.ErrorMessage);
    }

    [Fact]
    public async Task ThenWithParam_WhenStepThrowsException_ShouldReturnFailure()
    {
        // Arrange & Act
        var result = await PipelineBuilder<int>
            .Start(null, 10)
            .ThenWithParam(async (value, divisor, ct) =>
            {
                if (divisor == 0)
                    throw new DivideByZeroException("Cannot divide by zero");
                return FlowResult<int>.Success(value / divisor);
            }, 0)
            .ExecuteAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Cannot divide by zero", result.ErrorMessage);
        Assert.Equal("STEP_EXCEPTION", result.ErrorCode);
        Assert.NotNull(result.Exception);
        Assert.IsType<DivideByZeroException>(result.Exception);
    }

    [Fact]
    public async Task Pipeline_WithNestedException_ShouldPreserveExceptionHierarchy()
    {
        // Act
        var result = await PipelineBuilder<int>
            .Start(null, 5)
            .Then<int>(async (value, ct) =>
            {
                throw new InvalidOperationException("Outer failure", new ArgumentException("Inner failure"));
            })
            .ExecuteAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Exception);
        Assert.Equal("Outer failure", result.Exception!.Message);
        Assert.NotNull(result.Exception.InnerException);
        Assert.Equal("Inner failure", result.Exception.InnerException!.Message);
        Assert.NotNull(result.Failure);
        Assert.Same(result.Exception, result.Failure!.Exception);
    }

    [Fact]
    public async Task ThenWithParam_WithComplexParameter_ShouldWork()
    {
        // Arrange
        var config = new ValidationConfig
        {
            MinValue = 0,
            MaxValue = 100,
            ErrorMessage = "數值必須在 0-100 之間"
        };

        // Act
        var result = await PipelineBuilder<int>
            .Start(null, 50)
            .ThenWithParam(async (value, cfg, ct) =>
            {
                if (value < cfg.MinValue || value > cfg.MaxValue)
                    return FlowResult<int>.Fail(cfg.ErrorMessage, "VALIDATION_ERROR");
                return FlowResult<int>.Success(value);
            }, config)
            .ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(50, result.Value);
    }

    [Fact]
    public async Task DependencyInjection_ShouldReuseScopedServicesWithinSingleExecution()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ScopedExecutionDependency>();
        services.AddTransient<CaptureScopedIdStep>();
        services.AddTransient<VerifyScopedIdStep>();
        var serviceProvider = services.BuildServiceProvider();

        var pipeline = PipelineBuilder<string>
            .Start(serviceProvider, "start")
            .Then<CaptureScopedIdStep, string>()
            .Then<VerifyScopedIdStep, string>();

        // Act
        var firstRun = await pipeline.ExecuteAsync();
        var secondRun = await pipeline.ExecuteAsync();

        // Assert
        Assert.True(firstRun.IsSuccess);
        Assert.Equal("same", firstRun.Value);

        Assert.True(secondRun.IsSuccess);
        Assert.Equal("same", secondRun.Value);
    }
}

// Test helper classes for ThenWithParam tests
public class MultiplyByStep : IParameterizedPipelineStep<int, int, int>
{
    public Task<FlowResult<int>> ProcessAsync(int input, int multiplier, CancellationToken ct = default)
    {
        return Task.FromResult(FlowResult<int>.Success(input * multiplier));
    }
}

public class ValidationConfig
{
    public int MinValue { get; set; }
    public int MaxValue { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
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

public class TestError
{
    public string Message { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string TestProperty { get; init; } = string.Empty;
}

public enum TestErrorCode
{
    None = 0,
    ValidationFailed = 1
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

public sealed class TestObserver : IPipelineObserver
{
    public List<PipelineExecutionContext> ExecutionStarted { get; } = new();

    public List<PipelineExecutionContext> ExecutionCompleted { get; } = new();

    public List<PipelineStageContext> StageStarted { get; } = new();

    public List<PipelineStageContext> StageCompleted { get; } = new();

    public ValueTask OnExecutionStartedAsync(PipelineExecutionContext context, CancellationToken ct = default)
    {
        ExecutionStarted.Add(context);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnExecutionCompletedAsync(PipelineExecutionContext context, FlowFailure? failure, CancellationToken ct = default)
    {
        ExecutionCompleted.Add(context);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnStageStartedAsync(PipelineStageContext context, CancellationToken ct = default)
    {
        StageStarted.Add(context);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnStageCompletedAsync(PipelineStageContext context, FlowFailure? failure, CancellationToken ct = default)
    {
        StageCompleted.Add(context);
        return ValueTask.CompletedTask;
    }
}

public sealed class ThrowingObserver : IPipelineObserver
{
    public ValueTask OnExecutionStartedAsync(PipelineExecutionContext context, CancellationToken ct = default)
    {
        throw new InvalidOperationException("Observer failure");
    }
}

public sealed class ScopedExecutionDependency
{
    public Guid Id { get; } = Guid.NewGuid();
}

public class CaptureScopedIdStep : IPipelineStep<string, string>
{
    private readonly ScopedExecutionDependency _dependency;

    public CaptureScopedIdStep(ScopedExecutionDependency dependency)
    {
        _dependency = dependency;
    }

    public Task<FlowResult<string>> ProcessAsync(string input, CancellationToken ct = default)
    {
        return Task.FromResult(FlowResult<string>.Success(_dependency.Id.ToString()));
    }
}

public class VerifyScopedIdStep : IPipelineStep<string, string>
{
    private readonly ScopedExecutionDependency _dependency;

    public VerifyScopedIdStep(ScopedExecutionDependency dependency)
    {
        _dependency = dependency;
    }

    public Task<FlowResult<string>> ProcessAsync(string input, CancellationToken ct = default)
    {
        var currentId = _dependency.Id.ToString();
        var result = string.Equals(input, currentId, StringComparison.Ordinal) ? "same" : "different";
        return Task.FromResult(FlowResult<string>.Success(result));
    }
}
