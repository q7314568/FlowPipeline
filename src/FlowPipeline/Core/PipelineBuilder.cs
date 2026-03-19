using FlowPipeline.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FlowPipeline.Core;

/// <summary>
/// 使用流暢 API 建立和執行 Pipeline 的建構器。
/// </summary>
/// <typeparam name="TIn">此 Pipeline 階段的輸入型別。</typeparam>
public class PipelineBuilder<TIn>
{
    private readonly IServiceProvider? _serviceProvider;
    private readonly Func<IServiceProvider?, CancellationToken, Task<FlowResult<TIn>>> _pipeline;

    private PipelineBuilder(
        IServiceProvider? serviceProvider,
        Func<IServiceProvider?, CancellationToken, Task<FlowResult<TIn>>> pipeline)
    {
        _serviceProvider = serviceProvider;
        _pipeline = pipeline;
    }

    /// <summary>
    /// 建立一個從指定輸入值開始的新 Pipeline。
    /// </summary>
    /// <typeparam name="T">輸入型別。</typeparam>
    /// <param name="provider">用於依賴注入的選用 Service Provider。</param>
    /// <param name="input">初始輸入值。</param>
    /// <returns>新的 PipelineBuilder 實例。</returns>
    public static PipelineBuilder<T> Start<T>(IServiceProvider? provider, T input)
    {
        // 建立一個新的 PipelineBuilder，其 Pipeline 會立即返回包含初始輸入值的成功結果
        return new PipelineBuilder<T>(provider, (_, _) => Task.FromResult(FlowResult<T>.Success(input)));
    }

    /// <summary>
    /// 建立一個無輸入的新 Pipeline（使用 Unit）。
    /// </summary>
    /// <param name="provider">用於依賴注入的選用 Service Provider。</param>
    /// <returns>新的 PipelineBuilder 實例。</returns>
    public static PipelineBuilder<Unit> Start(IServiceProvider? provider)
    {
        // 呼叫 Start<T> 方法，並傳入 Unit.Value 作為初始值
        return Start(provider, Unit.Value);
    }

    /// <summary>
    /// 使用依賴注入新增一個步驟到 Pipeline。
    /// </summary>
    /// <typeparam name="TStep">要從 DI 容器解析的步驟型別。</typeparam>
    /// <typeparam name="TOut">步驟的輸出型別。</typeparam>
    /// <returns>下一階段的 PipelineBuilder。</returns>
    public PipelineBuilder<TOut> Then<TStep, TOut>()
        where TStep : IPipelineStep<TIn, TOut>
    {
        return CreateStepStage<TOut>(async (input, serviceProvider, ct) =>
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
    /// <returns>下一階段的 PipelineBuilder。</returns>
    public PipelineBuilder<TOut> Then<TOut>(IPipelineStep<TIn, TOut> stepInstance)
    {
        return CreateStepStage<TOut>((input, _, ct) =>
        {
            return stepInstance.ProcessAsync(input, ct);
        });
    }

    /// <summary>
    /// 使用 Lambda 函式新增一個步驟到 Pipeline。
    /// </summary>
    /// <typeparam name="TOut">步驟的輸出型別。</typeparam>
    /// <param name="next">要執行的函式。</param>
    /// <returns>下一階段的 PipelineBuilder。</returns>
    public PipelineBuilder<TOut> Then<TOut>(Func<TIn, CancellationToken, Task<FlowResult<TOut>>> next)
    {
        return CreateStepStage<TOut>((input, _, ct) =>
        {
            return next(input, ct);
        });
    }

    /// <summary>
    /// 使用帶有參數的步驟實例新增一個步驟到 Pipeline。
    /// </summary>
    /// <typeparam name="TOut">步驟的輸出型別。</typeparam>
    /// <typeparam name="TParam">額外參數的型別。</typeparam>
    /// <param name="stepInstance">要執行的帶參數步驟實例。</param>
    /// <param name="parameter">傳入步驟的額外參數。</param>
    /// <returns>下一階段的 PipelineBuilder。</returns>
    public PipelineBuilder<TOut> ThenWithParam<TOut, TParam>(
        IParameterizedPipelineStep<TIn, TOut, TParam> stepInstance,
        TParam parameter)
    {
        return CreateStepStage<TOut>((input, _, ct) =>
        {
            return stepInstance.ProcessAsync(input, parameter, ct);
        });
    }

    /// <summary>
    /// 使用 lambda 和額外參數新增一個步驟到 Pipeline。
    /// </summary>
    /// <typeparam name="TOut">步驟的輸出型別。</typeparam>
    /// <typeparam name="TParam">額外參數的型別。</typeparam>
    /// <param name="func">要執行的函式，接受輸入值、參數和取消權杖。</param>
    /// <param name="parameter">傳入函式的額外參數。</param>
    /// <returns>下一階段的 PipelineBuilder。</returns>
    public PipelineBuilder<TOut> ThenWithParam<TOut, TParam>(
        Func<TIn, TParam, CancellationToken, Task<FlowResult<TOut>>> func,
        TParam parameter)
    {
        return CreateStepStage<TOut>((input, _, ct) =>
        {
            return func(input, parameter, ct);
        });
    }

    /// <summary>
    /// 使用依賴注入新增一個條件式步驟到 Pipeline。
    /// </summary>
    /// <typeparam name="TStep">要從 DI 容器解析的步驟型別。</typeparam>
    /// <typeparam name="TOut">步驟的輸出型別。</typeparam>
    /// <param name="predicate">執行步驟前要檢查的條件。</param>
    /// <returns>下一階段的 PipelineBuilder。</returns>
    public PipelineBuilder<TOut> ThenWhen<TStep, TOut>(Func<TIn, bool> predicate)
        where TStep : IPipelineStep<TIn, TOut>
    {
        return CreateStepStage<TOut>(async (input, serviceProvider, ct) =>
        {
            if (!predicate(input))
            {
                return FlowResult<TOut>.Fail("Condition not met", "CONDITION_FAILED");
            }

            var step = ResolveRequired<TStep>(serviceProvider);
            return await step.ProcessAsync(input, ct);
        });
    }

    /// <summary>
    /// 使用 Lambda 函式新增一個條件式步驟到 Pipeline。
    /// </summary>
    /// <typeparam name="TOut">步驟的輸出型別。</typeparam>
    /// <param name="predicate">執行步驟前要檢查的條件。</param>
    /// <param name="next">當條件符合時要執行的函式。</param>
    /// <returns>下一階段的 PipelineBuilder。</returns>
    public PipelineBuilder<TOut> ThenWhen<TOut>(Func<TIn, bool> predicate, Func<TIn, CancellationToken, Task<FlowResult<TOut>>> next)
    {
        return CreateStepStage<TOut>(async (input, _, ct) =>
        {
            if (!predicate(input))
            {
                return FlowResult<TOut>.Fail("Condition not met", "CONDITION_FAILED");
            }

            return await next(input, ct);
        });
    }

    /// <summary>
    /// 使用依賴注入新增一個副作用動作到 Pipeline。
    /// 此動作會執行但不會改變 Pipeline 的值。
    /// </summary>
    /// <typeparam name="TStep">要從 DI 容器解析的動作型別。</typeparam>
    /// <returns>相同的 PipelineBuilder 實例。</returns>
    public PipelineBuilder<TIn> ThenDo<TStep>()
        where TStep : IPipelineAction<TIn>
    {
        return CreateActionStage(async (input, serviceProvider, ct) =>
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
    /// <returns>相同的 PipelineBuilder 實例。</returns>
    public PipelineBuilder<TIn> ThenDo(IPipelineAction<TIn> actionInstance)
    {
        return CreateActionStage((input, _, ct) =>
        {
            return actionInstance.ExecuteAsync(input, ct);
        });
    }

    /// <summary>
    /// 使用 Lambda 函式新增一個副作用動作到 Pipeline。
    /// 此動作會執行但不會改變 Pipeline 的值。
    /// </summary>
    /// <param name="action">要執行的函式。</param>
    /// <returns>相同的 PipelineBuilder 實例。</returns>
    public PipelineBuilder<TIn> ThenDo(Func<TIn, CancellationToken, Task> action)
    {
        return CreateActionStage((input, _, ct) =>
        {
            return action(input, ct);
        });
    }

    /// <summary>
    /// 使用依賴注入新增一個無參數動作到 Pipeline。
    /// 此動作會執行但不會改變 Pipeline 的值。
    /// </summary>
    /// <typeparam name="TStep">要從 DI 容器解析的動作型別。</typeparam>
    /// <returns>相同的 PipelineBuilder 實例。</returns>
    public PipelineBuilder<TIn> ThenRun<TStep>()
        where TStep : IPipelineAction
    {
        return CreateActionStage(async (_, serviceProvider, ct) =>
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
    /// <returns>相同的 PipelineBuilder 實例。</returns>
    public PipelineBuilder<TIn> ThenRun(IPipelineAction actionInstance)
    {
        return CreateActionStage((_, _, ct) =>
        {
            return actionInstance.ExecuteAsync(ct);
        });
    }

    /// <summary>
    /// 使用 Lambda 函式新增一個無參數動作到 Pipeline。
    /// 此動作會執行但不會改變 Pipeline 的值。
    /// </summary>
    /// <param name="action">要執行的函式。</param>
    /// <returns>相同的 PipelineBuilder 實例。</returns>
    public PipelineBuilder<TIn> ThenRun(Func<CancellationToken, Task> action)
    {
        return CreateActionStage((_, _, ct) =>
        {
            return action(ct);
        });
    }

    /// <summary>
    /// 執行整個 Pipeline 並返回最終結果。
    /// </summary>
    /// <param name="ct">取消權杖。</param>
    /// <returns>代表非同步操作的工作，包含最終結果。</returns>
    public async Task<FlowResult<TIn>> ExecuteAsync(CancellationToken ct = default)
    {
        // 每次執行共用同一個 DI Scope，確保 scoped service 在整條 pipeline 中一致
        if (_serviceProvider == null)
        {
            return await _pipeline(null, ct);
        }

        using var executionScope = _serviceProvider.CreateScope();
        return await _pipeline(executionScope.ServiceProvider, ct);
    }

    private PipelineBuilder<TOut> CreateStepStage<TOut>(
        Func<TIn, IServiceProvider?, CancellationToken, Task<FlowResult<TOut>>> next)
    {
        return new PipelineBuilder<TOut>(_serviceProvider, async (serviceProvider, ct) =>
        {
            var result = await _pipeline(serviceProvider, ct);

            if (!result.IsSuccess)
            {
                return FlowResult<TOut>.FromFailure(result);
            }

            try
            {
                return await next(result.Value!, serviceProvider, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                return FlowResult<TOut>.Fail($"Step execution failed: {ex.Message}", "STEP_EXCEPTION");
            }
        });
    }

    private PipelineBuilder<TIn> CreateActionStage(
        Func<TIn, IServiceProvider?, CancellationToken, Task> action)
    {
        return new PipelineBuilder<TIn>(_serviceProvider, async (serviceProvider, ct) =>
        {
            var result = await _pipeline(serviceProvider, ct);

            if (!result.IsSuccess)
            {
                return result;
            }

            try
            {
                await action(result.Value!, serviceProvider, ct);
                return result;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                return FlowResult<TIn>.Fail($"Action execution failed: {ex.Message}", "ACTION_EXCEPTION");
            }
        });
    }

    private static TService ResolveRequired<TService>(IServiceProvider? serviceProvider)
        where TService : notnull
    {
        if (serviceProvider == null)
        {
            throw new InvalidOperationException($"Cannot resolve {typeof(TService).Name} without a service provider");
        }

        return serviceProvider.GetRequiredService<TService>();
    }
}
