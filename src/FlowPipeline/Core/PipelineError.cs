namespace FlowPipeline.Core;

/// <summary>
/// Pipeline 錯誤的基底類別，提供結構化的錯誤資訊。
/// </summary>
public abstract class PipelineError
{
    /// <summary>
    /// 取得或初始化錯誤訊息。
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 取得或初始化錯誤代碼。
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// 取得或初始化錯誤發生的時間戳記。
    /// </summary>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
