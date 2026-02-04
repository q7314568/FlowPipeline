using FlowPipeline.Abstractions;
using FlowPipeline.Core;
using Microsoft.Extensions.DependencyInjection;

namespace FlowPipeline.Core;

/// <summary>
/// Builder for creating and executing pipelines with fluent API.
/// </summary>
/// <typeparam name="TIn">The input type for this pipeline stage.</typeparam>
public class PipelineBuilder<TIn>
{
    private readonly IServiceProvider? _serviceProvider;
    private readonly Func<CancellationToken, Task<FlowResult<TIn>>> _pipeline;

    private PipelineBuilder(IServiceProvider? serviceProvider, Func<CancellationToken, Task<FlowResult<TIn>>> pipeline)
    {
        _serviceProvider = serviceProvider;
        _pipeline = pipeline;
    }

    /// <summary>
    /// Creates a new pipeline starting with the specified input value.
    /// </summary>
    /// <typeparam name="T">The input type.</typeparam>
    /// <param name="provider">The optional service provider for dependency injection.</param>
    /// <param name="input">The initial input value.</param>
    /// <returns>A new PipelineBuilder instance.</returns>
    public static PipelineBuilder<T> Start<T>(IServiceProvider? provider, T input)
    {
        return new PipelineBuilder<T>(provider, _ => Task.FromResult(FlowResult<T>.Success(input)));
    }

    /// <summary>
    /// Creates a new pipeline without input (using Unit).
    /// </summary>
    /// <param name="provider">The optional service provider for dependency injection.</param>
    /// <returns>A new PipelineBuilder instance.</returns>
    public static PipelineBuilder<Unit> Start(IServiceProvider? provider)
    {
        return Start(provider, Unit.Value);
    }

    /// <summary>
    /// Adds a step to the pipeline using dependency injection.
    /// </summary>
    /// <typeparam name="TStep">The step type to resolve from DI.</typeparam>
    /// <typeparam name="TOut">The output type of the step.</typeparam>
    /// <returns>A new PipelineBuilder for the next stage.</returns>
    public PipelineBuilder<TOut> Then<TStep, TOut>()
        where TStep : IPipelineStep<TIn, TOut>
    {
        return new PipelineBuilder<TOut>(_serviceProvider, async ct =>
        {
            var result = await _pipeline(ct);
            if (!result.IsSuccess)
            {
                return FlowResult<TOut>.Fail(result.ErrorMessage ?? "Pipeline failed", result.ErrorCode);
            }

            try
            {
                if (_serviceProvider == null)
                {
                    throw new InvalidOperationException($"Cannot resolve {typeof(TStep).Name} without a service provider");
                }

                using var scope = _serviceProvider.CreateScope();
                var step = scope.ServiceProvider.GetRequiredService<TStep>();
                return await step.ProcessAsync(result.Value!, ct);
            }
            catch (Exception ex)
            {
                return FlowResult<TOut>.Fail($"Step execution failed: {ex.Message}", "STEP_EXCEPTION");
            }
        });
    }

    /// <summary>
    /// Adds a step to the pipeline using an instance.
    /// </summary>
    /// <typeparam name="TOut">The output type of the step.</typeparam>
    /// <param name="stepInstance">The step instance to execute.</param>
    /// <returns>A new PipelineBuilder for the next stage.</returns>
    public PipelineBuilder<TOut> Then<TOut>(IPipelineStep<TIn, TOut> stepInstance)
    {
        return new PipelineBuilder<TOut>(_serviceProvider, async ct =>
        {
            var result = await _pipeline(ct);
            if (!result.IsSuccess)
            {
                return FlowResult<TOut>.Fail(result.ErrorMessage ?? "Pipeline failed", result.ErrorCode);
            }

            try
            {
                return await stepInstance.ProcessAsync(result.Value!, ct);
            }
            catch (Exception ex)
            {
                return FlowResult<TOut>.Fail($"Step execution failed: {ex.Message}", "STEP_EXCEPTION");
            }
        });
    }

    /// <summary>
    /// Adds a step to the pipeline using a lambda function.
    /// </summary>
    /// <typeparam name="TOut">The output type of the step.</typeparam>
    /// <param name="next">The function to execute.</param>
    /// <returns>A new PipelineBuilder for the next stage.</returns>
    public PipelineBuilder<TOut> Then<TOut>(Func<TIn, CancellationToken, Task<FlowResult<TOut>>> next)
    {
        return new PipelineBuilder<TOut>(_serviceProvider, async ct =>
        {
            var result = await _pipeline(ct);
            if (!result.IsSuccess)
            {
                return FlowResult<TOut>.Fail(result.ErrorMessage ?? "Pipeline failed", result.ErrorCode);
            }

            try
            {
                return await next(result.Value!, ct);
            }
            catch (Exception ex)
            {
                return FlowResult<TOut>.Fail($"Step execution failed: {ex.Message}", "STEP_EXCEPTION");
            }
        });
    }

    /// <summary>
    /// Adds a conditional step to the pipeline using dependency injection.
    /// </summary>
    /// <typeparam name="TStep">The step type to resolve from DI.</typeparam>
    /// <typeparam name="TOut">The output type of the step.</typeparam>
    /// <param name="predicate">The condition to check before executing the step.</param>
    /// <returns>A new PipelineBuilder for the next stage.</returns>
    public PipelineBuilder<TOut> ThenWhen<TStep, TOut>(Func<TIn, bool> predicate)
        where TStep : IPipelineStep<TIn, TOut>
    {
        return new PipelineBuilder<TOut>(_serviceProvider, async ct =>
        {
            var result = await _pipeline(ct);
            if (!result.IsSuccess)
            {
                return FlowResult<TOut>.Fail(result.ErrorMessage ?? "Pipeline failed", result.ErrorCode);
            }

            if (!predicate(result.Value!))
            {
                return FlowResult<TOut>.Fail("Condition not met", "CONDITION_FAILED");
            }

            try
            {
                if (_serviceProvider == null)
                {
                    throw new InvalidOperationException($"Cannot resolve {typeof(TStep).Name} without a service provider");
                }

                using var scope = _serviceProvider.CreateScope();
                var step = scope.ServiceProvider.GetRequiredService<TStep>();
                return await step.ProcessAsync(result.Value!, ct);
            }
            catch (Exception ex)
            {
                return FlowResult<TOut>.Fail($"Step execution failed: {ex.Message}", "STEP_EXCEPTION");
            }
        });
    }

    /// <summary>
    /// Adds a conditional step to the pipeline using a lambda function.
    /// </summary>
    /// <typeparam name="TOut">The output type of the step.</typeparam>
    /// <param name="predicate">The condition to check before executing the step.</param>
    /// <param name="next">The function to execute if the condition is met.</param>
    /// <returns>A new PipelineBuilder for the next stage.</returns>
    public PipelineBuilder<TOut> ThenWhen<TOut>(Func<TIn, bool> predicate, Func<TIn, CancellationToken, Task<FlowResult<TOut>>> next)
    {
        return new PipelineBuilder<TOut>(_serviceProvider, async ct =>
        {
            var result = await _pipeline(ct);
            if (!result.IsSuccess)
            {
                return FlowResult<TOut>.Fail(result.ErrorMessage ?? "Pipeline failed", result.ErrorCode);
            }

            if (!predicate(result.Value!))
            {
                return FlowResult<TOut>.Fail("Condition not met", "CONDITION_FAILED");
            }

            try
            {
                return await next(result.Value!, ct);
            }
            catch (Exception ex)
            {
                return FlowResult<TOut>.Fail($"Step execution failed: {ex.Message}", "STEP_EXCEPTION");
            }
        });
    }

    /// <summary>
    /// Adds a side effect action to the pipeline using dependency injection.
    /// The action executes but doesn't change the pipeline value.
    /// </summary>
    /// <typeparam name="TStep">The action type to resolve from DI.</typeparam>
    /// <returns>The same PipelineBuilder instance.</returns>
    public PipelineBuilder<TIn> ThenDo<TStep>()
        where TStep : IPipelineAction<TIn>
    {
        return new PipelineBuilder<TIn>(_serviceProvider, async ct =>
        {
            var result = await _pipeline(ct);
            if (!result.IsSuccess)
            {
                return result;
            }

            try
            {
                if (_serviceProvider == null)
                {
                    throw new InvalidOperationException($"Cannot resolve {typeof(TStep).Name} without a service provider");
                }

                using var scope = _serviceProvider.CreateScope();
                var action = scope.ServiceProvider.GetRequiredService<TStep>();
                await action.ExecuteAsync(result.Value!, ct);
                return result;
            }
            catch (Exception ex)
            {
                return FlowResult<TIn>.Fail($"Action execution failed: {ex.Message}", "ACTION_EXCEPTION");
            }
        });
    }

    /// <summary>
    /// Adds a side effect action to the pipeline using an instance.
    /// The action executes but doesn't change the pipeline value.
    /// </summary>
    /// <param name="actionInstance">The action instance to execute.</param>
    /// <returns>The same PipelineBuilder instance.</returns>
    public PipelineBuilder<TIn> ThenDo(IPipelineAction<TIn> actionInstance)
    {
        return new PipelineBuilder<TIn>(_serviceProvider, async ct =>
        {
            var result = await _pipeline(ct);
            if (!result.IsSuccess)
            {
                return result;
            }

            try
            {
                await actionInstance.ExecuteAsync(result.Value!, ct);
                return result;
            }
            catch (Exception ex)
            {
                return FlowResult<TIn>.Fail($"Action execution failed: {ex.Message}", "ACTION_EXCEPTION");
            }
        });
    }

    /// <summary>
    /// Adds a side effect action to the pipeline using a lambda function.
    /// The action executes but doesn't change the pipeline value.
    /// </summary>
    /// <param name="action">The function to execute.</param>
    /// <returns>The same PipelineBuilder instance.</returns>
    public PipelineBuilder<TIn> ThenDo(Func<TIn, CancellationToken, Task> action)
    {
        return new PipelineBuilder<TIn>(_serviceProvider, async ct =>
        {
            var result = await _pipeline(ct);
            if (!result.IsSuccess)
            {
                return result;
            }

            try
            {
                await action(result.Value!, ct);
                return result;
            }
            catch (Exception ex)
            {
                return FlowResult<TIn>.Fail($"Action execution failed: {ex.Message}", "ACTION_EXCEPTION");
            }
        });
    }

    /// <summary>
    /// Adds a parameterless action to the pipeline using dependency injection.
    /// The action executes but doesn't change the pipeline value.
    /// </summary>
    /// <typeparam name="TStep">The action type to resolve from DI.</typeparam>
    /// <returns>The same PipelineBuilder instance.</returns>
    public PipelineBuilder<TIn> ThenRun<TStep>()
        where TStep : IPipelineAction
    {
        return new PipelineBuilder<TIn>(_serviceProvider, async ct =>
        {
            var result = await _pipeline(ct);
            if (!result.IsSuccess)
            {
                return result;
            }

            try
            {
                if (_serviceProvider == null)
                {
                    throw new InvalidOperationException($"Cannot resolve {typeof(TStep).Name} without a service provider");
                }

                using var scope = _serviceProvider.CreateScope();
                var action = scope.ServiceProvider.GetRequiredService<TStep>();
                await action.ExecuteAsync(ct);
                return result;
            }
            catch (Exception ex)
            {
                return FlowResult<TIn>.Fail($"Action execution failed: {ex.Message}", "ACTION_EXCEPTION");
            }
        });
    }

    /// <summary>
    /// Adds a parameterless action to the pipeline using an instance.
    /// The action executes but doesn't change the pipeline value.
    /// </summary>
    /// <param name="actionInstance">The action instance to execute.</param>
    /// <returns>The same PipelineBuilder instance.</returns>
    public PipelineBuilder<TIn> ThenRun(IPipelineAction actionInstance)
    {
        return new PipelineBuilder<TIn>(_serviceProvider, async ct =>
        {
            var result = await _pipeline(ct);
            if (!result.IsSuccess)
            {
                return result;
            }

            try
            {
                await actionInstance.ExecuteAsync(ct);
                return result;
            }
            catch (Exception ex)
            {
                return FlowResult<TIn>.Fail($"Action execution failed: {ex.Message}", "ACTION_EXCEPTION");
            }
        });
    }

    /// <summary>
    /// Adds a parameterless action to the pipeline using a lambda function.
    /// The action executes but doesn't change the pipeline value.
    /// </summary>
    /// <param name="action">The function to execute.</param>
    /// <returns>The same PipelineBuilder instance.</returns>
    public PipelineBuilder<TIn> ThenRun(Func<CancellationToken, Task> action)
    {
        return new PipelineBuilder<TIn>(_serviceProvider, async ct =>
        {
            var result = await _pipeline(ct);
            if (!result.IsSuccess)
            {
                return result;
            }

            try
            {
                await action(ct);
                return result;
            }
            catch (Exception ex)
            {
                return FlowResult<TIn>.Fail($"Action execution failed: {ex.Message}", "ACTION_EXCEPTION");
            }
        });
    }

    /// <summary>
    /// Executes the entire pipeline and returns the final result.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the final result.</returns>
    public Task<FlowResult<TIn>> ExecuteAsync(CancellationToken ct = default)
    {
        return _pipeline(ct);
    }
}
