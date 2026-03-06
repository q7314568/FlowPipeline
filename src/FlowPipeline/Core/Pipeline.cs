using FlowPipeline.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FlowPipeline.Core;

/// <summary>
/// 表示一個可執行的 Pipeline,支援鏈式操作。
/// </summary>
/// <typeparam name="TIn">此 Pipeline 階段的輸入型別。</typeparam>
public class Pipeline<TIn>
{
    private readonly IServiceProvider? _serviceProvider;
    private readonly Func<CancellationToken, Task<FlowResult<TIn>>> _pipeline;

    internal Pipeline(IServiceProvider? serviceProvider, Func<CancellationToken, Task<FlowResult<TIn>>> pipeline)
    {
        _serviceProvider = serviceProvider;
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
        return new Pipeline<TOut>(_serviceProvider, async ct =>
        {
            // 步驟 1: 執行前面累積的 Pipeline，取得結果
            var result = await _pipeline(ct);
            
            // 步驟 2: 若前面的步驟失敗，則進行短路處理，直接返回失敗結果
            if (!result.IsSuccess)
            {
                return FlowResult<TOut>.Fail(result.ErrorMessage ?? "Pipeline failed", result.ErrorCode);
            }

            try
            {
                // 步驟 3: 檢查是否有提供 ServiceProvider
                if (_serviceProvider == null)
                {
                    throw new InvalidOperationException($"Cannot resolve {typeof(TStep).Name} without a service provider");
                }

                // 步驟 4: 建立新的 DI Scope 並解析步驟實例
                using var scope = _serviceProvider.CreateScope();
                var step = scope.ServiceProvider.GetRequiredService<TStep>();
                
                // 步驟 5: 執行步驟並返回結果
                return await step.ProcessAsync(result.Value!, ct);
            }
            catch (Exception ex)
            {
                // 步驟 6: 若發生例外，將其包裝為失敗結果
                return FlowResult<TOut>.Fail($"Step execution failed: {ex.Message}", "STEP_EXCEPTION");
            }
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
        return new Pipeline<TOut>(_serviceProvider, async ct =>
        {
            // 步驟 1: 執行前面累積的 Pipeline，取得結果
            var result = await _pipeline(ct);
            
            // 步驟 2: 若前面的步驟失敗，則進行短路處理，直接返回失敗結果
            if (!result.IsSuccess)
            {
                return FlowResult<TOut>.Fail(result.ErrorMessage ?? "Pipeline failed", result.ErrorCode);
            }

            try
            {
                // 步驟 3: 使用提供的步驟實例執行處理，並返回結果
                return await stepInstance.ProcessAsync(result.Value!, ct);
            }
            catch (Exception ex)
            {
                // 步驟 4: 若發生例外，將其包裝為失敗結果
                return FlowResult<TOut>.Fail($"Step execution failed: {ex.Message}", "STEP_EXCEPTION");
            }
        });
    }

    /// <summary>
    /// 使用 Lambda 函式新增一個步驟到 Pipeline。
    /// </summary>
    /// <typeparam name="TOut">步驟的輸出型別。</typeparam>
    /// <param name="next">要執行的函式。</param>
    /// <returns>下一階段的 Pipeline。</returns>
    public Pipeline<TOut> Then<TOut>(Func<TIn, CancellationToken, Task<FlowResult<TOut>>> next)
    {
        return new Pipeline<TOut>(_serviceProvider, async ct =>
        {
            // 步驟 1: 執行前面累積的 Pipeline，取得結果
            var result = await _pipeline(ct);
            
            // 步驟 2: 若前面的步驟失敗，則進行短路處理，直接返回失敗結果
            if (!result.IsSuccess)
            {
                return FlowResult<TOut>.Fail(result.ErrorMessage ?? "Pipeline failed", result.ErrorCode);
            }

            try
            {
                // 步驟 3: 使用提供的 Lambda 函式執行處理，並返回結果
                return await next(result.Value!, ct);
            }
            catch (Exception ex)
            {
                // 步驟 4: 若發生例外，將其包裝為失敗結果
                return FlowResult<TOut>.Fail($"Step execution failed: {ex.Message}", "STEP_EXCEPTION");
            }
        });
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
        return new Pipeline<TOut>(_serviceProvider, async ct =>
        {
            // 步驟 1: 執行前面累積的 Pipeline，取得結果
            var result = await _pipeline(ct);

            // 步驟 2: 若前面的步驟失敗，則進行短路處理，直接返回失敗結果
            if (!result.IsSuccess)
            {
                return FlowResult<TOut>.Fail(result.ErrorMessage ?? "Pipeline failed", result.ErrorCode);
            }

            try
            {
                // 步驟 3: 使用提供的步驟實例和參數執行處理，並返回結果
                return await stepInstance.ProcessAsync(result.Value!, parameter, ct);
            }
            catch (Exception ex)
            {
                // 步驟 4: 若發生例外，將其包裝為失敗結果
                return FlowResult<TOut>.Fail($"Step execution failed: {ex.Message}", "STEP_EXCEPTION");
            }
        });
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
        return new Pipeline<TOut>(_serviceProvider, async ct =>
        {
            // 步驟 1: 執行前面累積的 Pipeline，取得結果
            var result = await _pipeline(ct);

            // 步驟 2: 若前面的步驟失敗，則進行短路處理，直接返回失敗結果
            if (!result.IsSuccess)
            {
                return FlowResult<TOut>.Fail(result.ErrorMessage ?? "Pipeline failed", result.ErrorCode);
            }

            try
            {
                // 步驟 3: 執行提供的函式並返回結果
                return await func(result.Value!, parameter, ct);
            }
            catch (Exception ex)
            {
                // 步驟 4: 若發生例外，將其包裝為失敗結果
                return FlowResult<TOut>.Fail($"Step execution failed: {ex.Message}", "STEP_EXCEPTION");
            }
        });
    }

    /// <summary>
    /// 使用依賴注入新增一個條件式步驟到 Pipeline。
    /// </summary>
    /// <typeparam name="TStep">要從 DI 容器解析的步驟型別。</typeparam>
    /// <typeparam name="TOut">步驟的輸出型別。</typeparam>
    /// <param name="predicate">執行步驟前要檢查的條件。</param>
    /// <returns>下一階段的 Pipeline。</returns>
    public Pipeline<TOut> ThenWhen<TStep, TOut>(Func<TIn, bool> predicate)
        where TStep : IPipelineStep<TIn, TOut>
    {
        return new Pipeline<TOut>(_serviceProvider, async ct =>
        {
            // 步驟 1: 執行前面累積的 Pipeline，取得結果
            var result = await _pipeline(ct);
            
            // 步驟 2: 若前面的步驟失敗，則進行短路處理，直接返回失敗結果
            if (!result.IsSuccess)
            {
                return FlowResult<TOut>.Fail(result.ErrorMessage ?? "Pipeline failed", result.ErrorCode);
            }

            // 步驟 3: 檢查條件是否符合，若不符合則返回失敗結果
            if (!predicate(result.Value!))
            {
                return FlowResult<TOut>.Fail("Condition not met", "CONDITION_FAILED");
            }

            try
            {
                // 步驟 4: 檢查是否有提供 ServiceProvider
                if (_serviceProvider == null)
                {
                    throw new InvalidOperationException($"Cannot resolve {typeof(TStep).Name} without a service provider");
                }

                // 步驟 5: 建立新的 DI Scope 並解析步驟實例
                using var scope = _serviceProvider.CreateScope();
                var step = scope.ServiceProvider.GetRequiredService<TStep>();
                
                // 步驟 6: 執行步驟並返回結果
                return await step.ProcessAsync(result.Value!, ct);
            }
            catch (Exception ex)
            {
                // 步驟 7: 若發生例外，將其包裝為失敗結果
                return FlowResult<TOut>.Fail($"Step execution failed: {ex.Message}", "STEP_EXCEPTION");
            }
        });
    }

    /// <summary>
    /// 使用 Lambda 函式新增一個條件式步驟到 Pipeline。
    /// </summary>
    /// <typeparam name="TOut">步驟的輸出型別。</typeparam>
    /// <param name="predicate">執行步驟前要檢查的條件。</param>
    /// <param name="next">當條件符合時要執行的函式。</param>
    /// <returns>下一階段的 Pipeline。</returns>
    public Pipeline<TOut> ThenWhen<TOut>(Func<TIn, bool> predicate, Func<TIn, CancellationToken, Task<FlowResult<TOut>>> next)
    {
        return new Pipeline<TOut>(_serviceProvider, async ct =>
        {
            // 步驟 1: 執行前面累積的 Pipeline，取得結果
            var result = await _pipeline(ct);
            
            // 步驟 2: 若前面的步驟失敗，則進行短路處理，直接返回失敗結果
            if (!result.IsSuccess)
            {
                return FlowResult<TOut>.Fail(result.ErrorMessage ?? "Pipeline failed", result.ErrorCode);
            }

            // 步驟 3: 檢查條件是否符合，若不符合則返回失敗結果
            if (!predicate(result.Value!))
            {
                return FlowResult<TOut>.Fail("Condition not met", "CONDITION_FAILED");
            }

            try
            {
                // 步驟 4: 使用提供的 Lambda 函式執行處理，並返回結果
                return await next(result.Value!, ct);
            }
            catch (Exception ex)
            {
                // 步驟 5: 若發生例外，將其包裝為失敗結果
                return FlowResult<TOut>.Fail($"Step execution failed: {ex.Message}", "STEP_EXCEPTION");
            }
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
        return new Pipeline<TIn>(_serviceProvider, async ct =>
        {
            // 步驟 1: 執行前面累積的 Pipeline，取得結果
            var result = await _pipeline(ct);
            
            // 步驟 2: 若前面的步驟失敗，則進行短路處理，直接返回失敗結果
            if (!result.IsSuccess)
            {
                return result;
            }

            try
            {
                // 步驟 3: 檢查是否有提供 ServiceProvider
                if (_serviceProvider == null)
                {
                    throw new InvalidOperationException($"Cannot resolve {typeof(TStep).Name} without a service provider");
                }

                // 步驟 4: 建立新的 DI Scope 並解析動作實例
                using var scope = _serviceProvider.CreateScope();
                var action = scope.ServiceProvider.GetRequiredService<TStep>();
                
                // 步驟 5: 執行動作（不改變 Pipeline 的值）
                await action.ExecuteAsync(result.Value!, ct);
                
                // 步驟 6: 返回原始結果，保持 Pipeline 的值不變
                return result;
            }
            catch (Exception ex)
            {
                // 步驟 7: 若發生例外，將其包裝為失敗結果
                return FlowResult<TIn>.Fail($"Action execution failed: {ex.Message}", "ACTION_EXCEPTION");
            }
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
        return new Pipeline<TIn>(_serviceProvider, async ct =>
        {
            // 步驟 1: 執行前面累積的 Pipeline，取得結果
            var result = await _pipeline(ct);
            
            // 步驟 2: 若前面的步驟失敗，則進行短路處理，直接返回失敗結果
            if (!result.IsSuccess)
            {
                return result;
            }

            try
            {
                // 步驟 3: 使用提供的動作實例執行動作（不改變 Pipeline 的值）
                await actionInstance.ExecuteAsync(result.Value!, ct);
                
                // 步驟 4: 返回原始結果，保持 Pipeline 的值不變
                return result;
            }
            catch (Exception ex)
            {
                // 步驟 5: 若發生例外，將其包裝為失敗結果
                return FlowResult<TIn>.Fail($"Action execution failed: {ex.Message}", "ACTION_EXCEPTION");
            }
        });
    }

    /// <summary>
    /// 使用 Lambda 函式新增一個副作用動作到 Pipeline。
    /// 此動作會執行但不會改變 Pipeline 的值。
    /// </summary>
    /// <param name="action">要執行的函式。</param>
    /// <returns>相同的 Pipeline 實例。</returns>
    public Pipeline<TIn> ThenDo(Func<TIn, CancellationToken, Task> action)
    {
        return new Pipeline<TIn>(_serviceProvider, async ct =>
        {
            // 步驟 1: 執行前面累積的 Pipeline，取得結果
            var result = await _pipeline(ct);
            
            // 步驟 2: 若前面的步驟失敗，則進行短路處理，直接返回失敗結果
            if (!result.IsSuccess)
            {
                return result;
            }

            try
            {
                // 步驟 3: 使用提供的 Lambda 函式執行動作（不改變 Pipeline 的值）
                await action(result.Value!, ct);
                
                // 步驟 4: 返回原始結果，保持 Pipeline 的值不變
                return result;
            }
            catch (Exception ex)
            {
                // 步驟 5: 若發生例外，將其包裝為失敗結果
                return FlowResult<TIn>.Fail($"Action execution failed: {ex.Message}", "ACTION_EXCEPTION");
            }
        });
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
        return new Pipeline<TIn>(_serviceProvider, async ct =>
        {
            // 步驟 1: 執行前面累積的 Pipeline，取得結果
            var result = await _pipeline(ct);
            
            // 步驟 2: 若前面的步驟失敗，則進行短路處理，直接返回失敗結果
            if (!result.IsSuccess)
            {
                return result;
            }

            try
            {
                // 步驟 3: 檢查是否有提供 ServiceProvider
                if (_serviceProvider == null)
                {
                    throw new InvalidOperationException($"Cannot resolve {typeof(TStep).Name} without a service provider");
                }

                // 步驟 4: 建立新的 DI Scope 並解析動作實例
                using var scope = _serviceProvider.CreateScope();
                var action = scope.ServiceProvider.GetRequiredService<TStep>();
                
                // 步驟 5: 執行無參數動作（不改變 Pipeline 的值）
                await action.ExecuteAsync(ct);
                
                // 步驟 6: 返回原始結果，保持 Pipeline 的值不變
                return result;
            }
            catch (Exception ex)
            {
                // 步驟 7: 若發生例外，將其包裝為失敗結果
                return FlowResult<TIn>.Fail($"Action execution failed: {ex.Message}", "ACTION_EXCEPTION");
            }
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
        return new Pipeline<TIn>(_serviceProvider, async ct =>
        {
            // 步驟 1: 執行前面累積的 Pipeline，取得結果
            var result = await _pipeline(ct);
            
            // 步驟 2: 若前面的步驟失敗，則進行短路處理，直接返回失敗結果
            if (!result.IsSuccess)
            {
                return result;
            }

            try
            {
                // 步驟 3: 使用提供的動作實例執行無參數動作（不改變 Pipeline 的值）
                await actionInstance.ExecuteAsync(ct);
                
                // 步驟 4: 返回原始結果，保持 Pipeline 的值不變
                return result;
            }
            catch (Exception ex)
            {
                // 步驟 5: 若發生例外，將其包裝為失敗結果
                return FlowResult<TIn>.Fail($"Action execution failed: {ex.Message}", "ACTION_EXCEPTION");
            }
        });
    }

    /// <summary>
    /// 使用 Lambda 函式新增一個無參數動作到 Pipeline。
    /// 此動作會執行但不會改變 Pipeline 的值。
    /// </summary>
    /// <param name="action">要執行的函式。</param>
    /// <returns>相同的 Pipeline 實例。</returns>
    public Pipeline<TIn> ThenRun(Func<CancellationToken, Task> action)
    {
        return new Pipeline<TIn>(_serviceProvider, async ct =>
        {
            // 步驟 1: 執行前面累積的 Pipeline，取得結果
            var result = await _pipeline(ct);
            
            // 步驟 2: 若前面的步驟失敗，則進行短路處理，直接返回失敗結果
            if (!result.IsSuccess)
            {
                return result;
            }

            try
            {
                // 步驟 3: 使用提供的 Lambda 函式執行無參數動作（不改變 Pipeline 的值）
                await action(ct);
                
                // 步驟 4: 返回原始結果，保持 Pipeline 的值不變
                return result;
            }
            catch (Exception ex)
            {
                // 步驟 5: 若發生例外，將其包裝為失敗結果
                return FlowResult<TIn>.Fail($"Action execution failed: {ex.Message}", "ACTION_EXCEPTION");
            }
        });
    }

    /// <summary>
    /// 執行整個 Pipeline 並返回最終結果。
    /// </summary>
    /// <param name="ct">取消權杖。</param>
    /// <returns>代表非同步操作的工作，包含最終結果。</returns>
    public Task<FlowResult<TIn>> ExecuteAsync(CancellationToken ct = default)
    {
        // 執行完整的 Pipeline 函式並返回結果
        return _pipeline(ct);
    }
}
