namespace FlowPipeline.Core;

/// <summary>
/// Pipeline 建構器,用於建立 Pipeline 執行鏈。
/// </summary>
public static class PipelineBuilder
{
    /// <summary>
    /// 建立一個從指定輸入值開始的新 Pipeline。
    /// </summary>
    /// <typeparam name="T">輸入型別。</typeparam>
    /// <param name="provider">用於依賴注入的選用 Service Provider。</param>
    /// <param name="input">初始輸入值。</param>
    /// <returns>新的 Pipeline 實例。</returns>
    public static Pipeline<T> Start<T>(IServiceProvider? provider, T input)
    {
        // 建立一個新的 Pipeline，其 Pipeline 會立即返回包含初始輸入值的成功結果
        return new Pipeline<T>(provider, _ => Task.FromResult(FlowResult<T>.Success(input)));
    }
}
