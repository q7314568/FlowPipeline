using FlowPipeline.Core;

namespace FlowPipeline.Extensions;

/// <summary>
/// PipelineBuilder 的擴充方法。
/// </summary>
public static class PipelineBuilderExtensions
{
    /// <summary>
    /// 使用轉換函式映射目前 Pipeline 的值。
    /// </summary>
    /// <typeparam name="T">要轉換的值的型別。</typeparam>
    /// <param name="builder">Pipeline 建構器。</param>
    /// <param name="transform">轉換函式。</param>
    /// <returns>包含轉換後值的新 PipelineBuilder。</returns>
    public static PipelineBuilder<T> Map<T>(this PipelineBuilder<T> builder, Func<T, T> transform)
    {
        return builder.Then((input, ct) =>
        {
            // 使用轉換函式處理輸入值
            var transformed = transform(input);
            // 將轉換後的值包裝為成功結果並返回
            return Task.FromResult(FlowResult<T>.Success(transformed));
        });
    }
}
