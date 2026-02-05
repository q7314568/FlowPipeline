namespace FlowPipeline.Abstractions;

/// <summary>
/// 表示一個執行副作用並接受 TIn 型別輸入的 Pipeline 動作。
/// </summary>
/// <typeparam name="TIn">輸入型別。</typeparam>
public interface IPipelineAction<TIn>
{
    /// <summary>
    /// 使用提供的輸入以非同步方式執行動作。
    /// </summary>
    /// <param name="input">輸入值。</param>
    /// <param name="ct">取消權杖。</param>
    /// <returns>代表非同步操作的工作。</returns>
    Task ExecuteAsync(TIn input, CancellationToken ct = default);
}

/// <summary>
/// 表示一個執行副作用但不接受輸入的 Pipeline 動作。
/// </summary>
public interface IPipelineAction
{
    /// <summary>
    /// 以非同步方式執行動作。
    /// </summary>
    /// <param name="ct">取消權杖。</param>
    /// <returns>代表非同步操作的工作。</returns>
    Task ExecuteAsync(CancellationToken ct = default);
}
