using System.ComponentModel;

namespace FlowPipeline.Core;

/// <summary>
/// 提供建立 Pipeline 的新入口。
/// </summary>
public static class PipelineBuilder
{
    /// <summary>
    /// 建立一個從指定輸入值開始的新 Pipeline。
    /// </summary>
    /// <typeparam name="T">輸入型別。</typeparam>
    /// <param name="provider">用於依賴注入的選用 Service Provider。</param>
    /// <param name="input">初始輸入值。</param>
    /// <param name="options">Pipeline 執行選項。</param>
    /// <returns>新的 Pipeline 實例。</returns>
    public static Pipeline<T> Start<T>(IServiceProvider? provider, T input, PipelineOptions? options = null)
    {
        return new Pipeline<T>(provider, options, 0, (_, _) => Task.FromResult(FlowResult<T>.Success(input)));
    }

    /// <summary>
    /// 建立一個無輸入的新 Pipeline（使用 Unit）。
    /// </summary>
    /// <param name="provider">用於依賴注入的選用 Service Provider。</param>
    /// <param name="options">Pipeline 執行選項。</param>
    /// <returns>新的 Pipeline 實例。</returns>
    public static Pipeline<Unit> Start(IServiceProvider? provider, PipelineOptions? options = null)
    {
        return Start(provider, Unit.Value, options);
    }
}

/// <summary>
/// 舊版相容用的 Pipeline 建構器入口。
/// </summary>
/// <typeparam name="TIn">此 Pipeline 階段的輸入型別。</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public class PipelineBuilder<TIn> : Pipeline<TIn>
{
    private PipelineBuilder(
        IServiceProvider? serviceProvider,
        PipelineOptions? options,
        int stageCount,
        Func<PipelineExecutionState, CancellationToken, Task<FlowResult<TIn>>> pipeline)
        : base(serviceProvider, options, stageCount, pipeline)
    {
    }

    /// <summary>
    /// 建立一個從指定輸入值開始的新 Pipeline。
    /// </summary>
    /// <typeparam name="T">輸入型別。</typeparam>
    /// <param name="provider">用於依賴注入的選用 Service Provider。</param>
    /// <param name="input">初始輸入值。</param>
    /// <param name="options">Pipeline 執行選項。</param>
    /// <returns>新的 PipelineBuilder 實例。</returns>
    public static PipelineBuilder<T> Start<T>(IServiceProvider? provider, T input, PipelineOptions? options = null)
    {
        return new PipelineBuilder<T>(provider, options, 0, (_, _) => Task.FromResult(FlowResult<T>.Success(input)));
    }

    /// <summary>
    /// 建立一個無輸入的新 Pipeline（使用 Unit）。
    /// </summary>
    /// <param name="provider">用於依賴注入的選用 Service Provider。</param>
    /// <param name="options">Pipeline 執行選項。</param>
    /// <returns>新的 PipelineBuilder 實例。</returns>
    public static PipelineBuilder<Unit> Start(IServiceProvider? provider, PipelineOptions? options = null)
    {
        return Start(provider, Unit.Value, options);
    }
}
