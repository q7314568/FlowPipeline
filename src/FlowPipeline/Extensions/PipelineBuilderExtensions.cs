using FlowPipeline.Core;

namespace FlowPipeline.Extensions;

/// <summary>
/// Pipeline 的擴充方法。
/// </summary>
public static class PipelineBuilderExtensions
{
    /// <summary>
    /// 使用轉換函式映射目前 Pipeline 的值。
    /// </summary>
    /// <typeparam name="T">要轉換的值的型別。</typeparam>
    /// <param name="pipeline">Pipeline 實例。</param>
    /// <param name="transform">轉換函式。</param>
    /// <returns>包含轉換後值的新 Pipeline。</returns>
    public static Pipeline<T> Map<T>(this Pipeline<T> pipeline, Func<T, T> transform)
    {
        return pipeline.Then((input, ct) =>
        {
            // 使用轉換函式處理輸入值
            var transformed = transform(input);
            // 將轉換後的值包裝為成功結果並返回
            return Task.FromResult(FlowResult<T>.Success(transformed));
        });
    }
}
