using FlowPipeline.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FlowPipeline.Core;

/// <summary>
/// 表示一個可執行的 Pipeline，支援鏈式操作。
/// </summary>
/// <typeparam name="TIn">此 Pipeline 階段的輸入型別。</typeparam>
public class Pipeline<TIn>
{
    private readonly IServiceProvider? _serviceProvider;
    private readonly PipelineOptions _options;
    private readonly int _stageCount;
    private readonly Func<PipelineExecutionState, CancellationToken, Task<FlowResult<TIn>>> _pipeline;

    /// <summary>
    /// 初始化 <see cref="Pipeline{TIn}"/> 類別的新執行節點。
    /// </summary>
    /// <param name="serviceProvider">提供 DI 解析能力的服務提供者。</param>
    /// <param name="options">Pipeline 執行選項。</param>
    /// <param name="stageCount">目前已建立的階段數。</param>
    /// <param name="pipeline">封裝目前 Pipeline 執行流程的委派。</param>
    internal Pipeline(
        IServiceProvider? serviceProvider,
        PipelineOptions? options,
        int stageCount,
        Func<PipelineExecutionState, CancellationToken, Task<FlowResult<TIn>>> pipeline)
    {
        _serviceProvider = serviceProvider;
        _options = options ?? new PipelineOptions();
        _stageCount = stageCount;
        _pipeline = pipeline;
    }

    /// <summary>
    /// 使用依賴注入新增一個步驟到 Pipeline。
    /// </summary>
    /// <typeparam name="TStep">要從 DI 容器解析的步驟型別。</typeparam>
    /// <typeparam name="TOut">步驟的輸出型別。</typeparam>
    /// <returns>下一階段的 Pipeline。</returns>
    public Pipeline<TOut> Then<TStep, TOut>()
        where TStep : IPipelineStep<TIn, TOut>
    {
        return CreateStepStage(
            typeof(TStep).Name,
            async (input, serviceProvider, ct) =>
            {
                var step = ResolveRequired<TStep>(serviceProvider);
                return await step.ProcessAsync(input, ct);
            });
    }

    /// <summary>
    /// 使用步驟實例新增一個步驟到 Pipeline。
    /// </summary>
    /// <typeparam name="TOut">步驟的輸出型別。</typeparam>
    /// <param name="stepInstance">要執行的步驟實例。</param>
    /// <returns>下一階段的 Pipeline。</returns>
    public Pipeline<TOut> Then<TOut>(IPipelineStep<TIn, TOut> stepInstance)
    {
        ArgumentNullException.ThrowIfNull(stepInstance);

        return CreateStepStage(stepInstance.GetType().Name, (input, _, ct) => stepInstance.ProcessAsync(input, ct));
    }

    /// <summary>
    /// 使用 Lambda 函式新增一個步驟到 Pipeline。
    /// </summary>
    /// <typeparam name="TOut">步驟的輸出型別。</typeparam>
    /// <param name="next">要執行的函式。</param>
    /// <returns>下一階段的 Pipeline。</returns>
    public Pipeline<TOut> Then<TOut>(Func<TIn, CancellationToken, Task<FlowResult<TOut>>> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return CreateStepStage(GetDelegateName(next, "LambdaStep"), (input, _, ct) => next(input, ct));
    }

    /// <summary>
    /// 使用帶有參數的步驟實例新增一個步驟到 Pipeline。
    /// </summary>
    /// <typeparam name="TOut">步驟的輸出型別。</typeparam>
    /// <typeparam name="TParam">額外參數的型別。</typeparam>
    /// <param name="stepInstance">要執行的帶參數步驟實例。</param>
    /// <param name="parameter">傳入步驟的額外參數。</param>
    /// <returns>下一階段的 Pipeline。</returns>
    public Pipeline<TOut> ThenWithParam<TOut, TParam>(
        IParameterizedPipelineStep<TIn, TOut, TParam> stepInstance,
        TParam parameter)
    {
        ArgumentNullException.ThrowIfNull(stepInstance);

        return CreateStepStage(
            stepInstance.GetType().Name,
            (input, _, ct) => stepInstance.ProcessAsync(input, parameter, ct));
    }

    /// <summary>
    /// 使用 lambda 和額外參數新增一個步驟到 Pipeline。
    /// </summary>
    /// <typeparam name="TOut">步驟的輸出型別。</typeparam>
    /// <typeparam name="TParam">額外參數的型別。</typeparam>
    /// <param name="func">要執行的函式，接受輸入值、參數和取消權杖。</param>
    /// <param name="parameter">傳入函式的額外參數。</param>
    /// <returns>下一階段的 Pipeline。</returns>
    public Pipeline<TOut> ThenWithParam<TOut, TParam>(
        Func<TIn, TParam, CancellationToken, Task<FlowResult<TOut>>> func,
        TParam parameter)
    {
        ArgumentNullException.ThrowIfNull(func);

        return CreateStepStage(
            GetDelegateName(func, "LambdaStepWithParam"),
            (input, _, ct) => func(input, parameter, ct));
    }

    /// <summary>
    /// 使用依賴注入新增一個條件式步驟到 Pipeline。
    /// 條件不成立時會略過此步驟，並將目前值繼續傳遞到後續階段。
    /// </summary>
    /// <typeparam name="TStep">要從 DI 容器解析的步驟型別。</typeparam>
    /// <param name="predicate">執行步驟前要檢查的條件。</param>
    /// <returns>相同型別的下一階段 Pipeline。</returns>
    public Pipeline<TIn> ThenWhen<TStep>(Func<TIn, bool> predicate)
        where TStep : IPipelineStep<TIn, TIn>
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return CreateStepStage(
            $"Conditional:{typeof(TStep).Name}",
            async (input, serviceProvider, ct) =>
            {
                if (!predicate(input))
                {
                    return FlowResult<TIn>.Success(input);
                }

                var step = ResolveRequired<TStep>(serviceProvider);
                return await step.ProcessAsync(input, ct);
            });
    }

    /// <summary>
    /// 使用 Lambda 函式新增一個條件式步驟到 Pipeline。
    /// 條件不成立時會略過此步驟，並將目前值繼續傳遞到後續階段。
    /// </summary>
    /// <param name="predicate">執行步驟前要檢查的條件。</param>
    /// <param name="next">當條件符合時要執行的函式。</param>
    /// <returns>相同型別的下一階段 Pipeline。</returns>
    public Pipeline<TIn> ThenWhen(
        Func<TIn, bool> predicate,
        Func<TIn, CancellationToken, Task<FlowResult<TIn>>> next)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(next);

        return CreateStepStage(
            $"Conditional:{GetDelegateName(next, "LambdaStep")}",
            async (input, _, ct) =>
            {
                if (!predicate(input))
                {
                    return FlowResult<TIn>.Success(input);
                }

                return await next(input, ct);
            });
    }

    /// <summary>
    /// 使用依賴注入新增一個副作用動作到 Pipeline。
    /// 此動作會執行但不會改變 Pipeline 的值。
    /// </summary>
    /// <typeparam name="TStep">要從 DI 容器解析的動作型別。</typeparam>
    /// <returns>相同的 Pipeline 實例。</returns>
    public Pipeline<TIn> ThenDo<TStep>()
        where TStep : IPipelineAction<TIn>
    {
        return CreateActionStage(
            typeof(TStep).Name,
            async (input, serviceProvider, ct) =>
            {
                var action = ResolveRequired<TStep>(serviceProvider);
                await action.ExecuteAsync(input, ct);
            });
    }

    /// <summary>
    /// 使用動作實例新增一個副作用動作到 Pipeline。
    /// 此動作會執行但不會改變 Pipeline 的值。
    /// </summary>
    /// <param name="actionInstance">要執行的動作實例。</param>
    /// <returns>相同的 Pipeline 實例。</returns>
    public Pipeline<TIn> ThenDo(IPipelineAction<TIn> actionInstance)
    {
        ArgumentNullException.ThrowIfNull(actionInstance);

        return CreateActionStage(actionInstance.GetType().Name, (input, _, ct) => actionInstance.ExecuteAsync(input, ct));
    }

    /// <summary>
    /// 使用 Lambda 函式新增一個副作用動作到 Pipeline。
    /// 此動作會執行但不會改變 Pipeline 的值。
    /// </summary>
    /// <param name="action">要執行的函式。</param>
    /// <returns>相同的 Pipeline 實例。</returns>
    public Pipeline<TIn> ThenDo(Func<TIn, CancellationToken, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return CreateActionStage(GetDelegateName(action, "LambdaAction"), (input, _, ct) => action(input, ct));
    }

    /// <summary>
    /// 使用依賴注入新增一個無參數動作到 Pipeline。
    /// 此動作會執行但不會改變 Pipeline 的值。
    /// </summary>
    /// <typeparam name="TStep">要從 DI 容器解析的動作型別。</typeparam>
    /// <returns>相同的 Pipeline 實例。</returns>
    public Pipeline<TIn> ThenRun<TStep>()
        where TStep : IPipelineAction
    {
        return CreateActionStage(
            typeof(TStep).Name,
            async (_, serviceProvider, ct) =>
            {
                var action = ResolveRequired<TStep>(serviceProvider);
                await action.ExecuteAsync(ct);
            });
    }

    /// <summary>
    /// 使用動作實例新增一個無參數動作到 Pipeline。
    /// 此動作會執行但不會改變 Pipeline 的值。
    /// </summary>
    /// <param name="actionInstance">要執行的動作實例。</param>
    /// <returns>相同的 Pipeline 實例。</returns>
    public Pipeline<TIn> ThenRun(IPipelineAction actionInstance)
    {
        ArgumentNullException.ThrowIfNull(actionInstance);

        return CreateActionStage(actionInstance.GetType().Name, (_, _, ct) => actionInstance.ExecuteAsync(ct));
    }

    /// <summary>
    /// 使用 Lambda 函式新增一個無參數動作到 Pipeline。
    /// 此動作會執行但不會改變 Pipeline 的值。
    /// </summary>
    /// <param name="action">要執行的函式。</param>
    /// <returns>相同的 Pipeline 實例。</returns>
    public Pipeline<TIn> ThenRun(Func<CancellationToken, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return CreateActionStage(GetDelegateName(action, "LambdaAction"), (_, _, ct) => action(ct));
    }

    /// <summary>
    /// 執行整個 Pipeline 並返回最終結果。
    /// </summary>
    /// <param name="ct">取消權杖。</param>
    /// <returns>代表非同步操作的工作，包含最終結果。</returns>
    public async Task<FlowResult<TIn>> ExecuteAsync(CancellationToken ct = default)
    {
        var executionContext = new PipelineExecutionContext(
            Guid.NewGuid(),
            _options.Name ?? typeof(TIn).Name,
            _stageCount);

        await NotifyExecutionStartedAsync(executionContext, ct);

        try
        {
            FlowResult<TIn> result;

            if (_serviceProvider == null)
            {
                result = await _pipeline(new PipelineExecutionState(null, executionContext), ct);
            }
            else
            {
                using var executionScope = _serviceProvider.CreateScope();
                result = await _pipeline(new PipelineExecutionState(executionScope.ServiceProvider, executionContext), ct);
            }

            await NotifyExecutionCompletedAsync(executionContext, result.Failure, ct);
            return result;
        }
        catch (OperationCanceledException ex) when (ct.IsCancellationRequested)
        {
            await NotifyExecutionCompletedAsync(
                executionContext,
                new FlowFailure("Pipeline execution cancelled", "PIPELINE_CANCELLED", exception: ex),
                CancellationToken.None);
            throw;
        }
    }

    /// <summary>
    /// 建立新的步驟節點，並在前一階段成功時執行下一個轉換。
    /// </summary>
    /// <typeparam name="TOut">下一階段的輸出型別。</typeparam>
    /// <param name="stageName">階段名稱。</param>
    /// <param name="next">實際執行下一階段的委派。</param>
    /// <returns>代表下一階段的新 Pipeline。</returns>
    private Pipeline<TOut> CreateStepStage<TOut>(
        string stageName,
        Func<TIn, IServiceProvider?, CancellationToken, Task<FlowResult<TOut>>> next)
    {
        var stageIndex = _stageCount + 1;

        return new Pipeline<TOut>(_serviceProvider, _options, stageIndex, async (state, ct) =>
        {
            var result = await _pipeline(state, ct);

            if (!result.IsSuccess)
            {
                return FlowResult<TOut>.FromFailure(result);
            }

            return await ExecuteStageWithPoliciesAsync(
                state,
                stageIndex,
                stageName,
                PipelineStageKind.Step,
                typeof(TIn),
                typeof(TOut),
                ct,
                stageCt => next(result.Value!, state.ServiceProvider, stageCt),
                "STEP_EXCEPTION",
                "Step execution failed");
        });
    }

    /// <summary>
    /// 建立新的副作用節點，並在前一階段成功時執行指定動作。
    /// </summary>
    /// <param name="stageName">階段名稱。</param>
    /// <param name="action">實際執行副作用的委派。</param>
    /// <returns>維持相同輸入型別的 Pipeline。</returns>
    private Pipeline<TIn> CreateActionStage(
        string stageName,
        Func<TIn, IServiceProvider?, CancellationToken, Task> action)
    {
        var stageIndex = _stageCount + 1;

        return new Pipeline<TIn>(_serviceProvider, _options, stageIndex, async (state, ct) =>
        {
            var result = await _pipeline(state, ct);

            if (!result.IsSuccess)
            {
                return result;
            }

            return await ExecuteStageWithPoliciesAsync(
                state,
                stageIndex,
                stageName,
                PipelineStageKind.Action,
                typeof(TIn),
                typeof(TIn),
                ct,
                async stageCt =>
                {
                    await action(result.Value!, state.ServiceProvider, stageCt);
                    return result;
                },
                "ACTION_EXCEPTION",
                "Action execution failed");
        });
    }

    private async Task<FlowResult<TOut>> ExecuteStageWithPoliciesAsync<TOut>(
        PipelineExecutionState state,
        int stageIndex,
        string stageName,
        PipelineStageKind stageKind,
        Type inputType,
        Type outputType,
        CancellationToken ct,
        Func<CancellationToken, Task<FlowResult<TOut>>> operation,
        string defaultCode,
        string defaultMessagePrefix)
    {
        var retryOptions = _options.Retry;
        var maxAttempts = Math.Max(retryOptions?.MaxAttempts ?? 1, 1);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var stageContext = new PipelineStageContext(
                state.Execution,
                stageIndex,
                stageName,
                stageKind,
                inputType,
                outputType,
                attempt,
                _options.StageTimeout);

            await NotifyStageStartedAsync(stageContext, ct);

            try
            {
                var result = await ExecuteWithTimeoutAsync(operation, stageContext, ct);

                await NotifyStageCompletedAsync(stageContext, result.Failure, ct);

                if (!result.IsSuccess &&
                    attempt < maxAttempts &&
                    result.Failure != null &&
                    ShouldRetryFailure(retryOptions, result.Failure))
                {
                    await DelayBeforeRetryAsync(retryOptions, ct);
                    continue;
                }

                return result;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (TimeoutException ex)
            {
                var failure = MapFailure(
                    stageContext,
                    ex,
                    $"{stageName} timed out after {_options.StageTimeout}.",
                    "STAGE_TIMEOUT",
                    isTimeout: true);

                await NotifyStageCompletedAsync(stageContext, failure, ct);

                if (attempt < maxAttempts && ShouldRetryFailure(retryOptions, failure))
                {
                    await DelayBeforeRetryAsync(retryOptions, ct);
                    continue;
                }

                return FlowResult<TOut>.Fail(failure);
            }
            catch (Exception ex)
            {
                var failure = MapFailure(
                    stageContext,
                    ex,
                    $"{defaultMessagePrefix}: {ex.Message}",
                    defaultCode,
                    isTimeout: false);

                await NotifyStageCompletedAsync(stageContext, failure, ct);

                if (attempt < maxAttempts && ShouldRetryException(retryOptions, ex))
                {
                    await DelayBeforeRetryAsync(retryOptions, ct);
                    continue;
                }

                return FlowResult<TOut>.Fail(failure);
            }
        }

        throw new InvalidOperationException("Stage execution ended without a terminal result.");
    }

    private async Task<FlowResult<TOut>> ExecuteWithTimeoutAsync<TOut>(
        Func<CancellationToken, Task<FlowResult<TOut>>> operation,
        PipelineStageContext stageContext,
        CancellationToken ct)
    {
        if (_options.StageTimeout is not { } timeout)
        {
            return await operation(ct);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout);

        try
        {
            return await operation(timeoutCts.Token);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested && timeoutCts.IsCancellationRequested)
        {
            throw new TimeoutException($"{stageContext.StageName} timed out after {timeout}.", ex);
        }
    }

    private FlowFailure MapFailure(
        PipelineStageContext stageContext,
        Exception exception,
        string defaultMessage,
        string defaultCode,
        bool isTimeout)
    {
        var context = new PipelineFailureMappingContext(stageContext, exception, defaultMessage, defaultCode, isTimeout);
        return _options.FailureMapper?.Invoke(context) ?? new FlowFailure(defaultMessage, defaultCode, exception: exception);
    }

    private async Task NotifyExecutionStartedAsync(PipelineExecutionContext context, CancellationToken ct)
    {
        foreach (var observer in _options.Observers)
        {
            try
            {
                await observer.OnExecutionStartedAsync(context, ct);
            }
            catch
            {
                // Observability hooks should not break business execution.
            }
        }
    }

    private async Task NotifyExecutionCompletedAsync(PipelineExecutionContext context, FlowFailure? failure, CancellationToken ct)
    {
        foreach (var observer in _options.Observers)
        {
            try
            {
                await observer.OnExecutionCompletedAsync(context, failure, ct);
            }
            catch
            {
                // Observability hooks should not break business execution.
            }
        }
    }

    private async Task NotifyStageStartedAsync(PipelineStageContext context, CancellationToken ct)
    {
        foreach (var observer in _options.Observers)
        {
            try
            {
                await observer.OnStageStartedAsync(context, ct);
            }
            catch
            {
                // Observability hooks should not break business execution.
            }
        }
    }

    private async Task NotifyStageCompletedAsync(PipelineStageContext context, FlowFailure? failure, CancellationToken ct)
    {
        foreach (var observer in _options.Observers)
        {
            try
            {
                await observer.OnStageCompletedAsync(context, failure, ct);
            }
            catch
            {
                // Observability hooks should not break business execution.
            }
        }
    }

    private static bool ShouldRetryException(PipelineRetryOptions? retryOptions, Exception exception)
    {
        if (retryOptions == null || exception is OperationCanceledException)
        {
            return false;
        }

        return retryOptions.ShouldRetryException?.Invoke(exception) ?? true;
    }

    private static bool ShouldRetryFailure(PipelineRetryOptions? retryOptions, FlowFailure failure)
    {
        if (retryOptions == null)
        {
            return false;
        }

        return retryOptions.ShouldRetryFailure?.Invoke(failure) ?? false;
    }

    private static async Task DelayBeforeRetryAsync(PipelineRetryOptions? retryOptions, CancellationToken ct)
    {
        if (retryOptions == null || retryOptions.Delay <= TimeSpan.Zero)
        {
            return;
        }

        await Task.Delay(retryOptions.Delay, ct);
    }

    /// <summary>
    /// 從服務提供者解析指定型別，若未提供服務提供者則拋出例外。
    /// </summary>
    /// <typeparam name="TService">要解析的服務型別。</typeparam>
    /// <param name="serviceProvider">目前 Pipeline 執行所使用的服務提供者。</param>
    /// <returns>解析後的服務實例。</returns>
    /// <exception cref="InvalidOperationException">當未提供服務提供者時拋出。</exception>
    private static TService ResolveRequired<TService>(IServiceProvider? serviceProvider)
        where TService : notnull
    {
        if (serviceProvider == null)
        {
            throw new InvalidOperationException($"Cannot resolve {typeof(TService).Name} without a service provider");
        }

        return serviceProvider.GetRequiredService<TService>();
    }

    private static string GetDelegateName(Delegate del, string fallback)
    {
        var methodName = del.Method.Name;
        return methodName.StartsWith("<", StringComparison.Ordinal) ? fallback : methodName;
    }
}
