namespace FlowPipeline.Core;

/// <summary>
/// 定義 pipeline 階段的重試行為。
/// </summary>
public sealed class PipelineRetryOptions
{
    /// <summary>
    /// 取得或設定每個階段的最大嘗試次數，至少為 1。
    /// </summary>
    public int MaxAttempts { get; set; } = 1;

    /// <summary>
    /// 取得或設定每次重試前要等待的時間。
    /// </summary>
    public TimeSpan Delay { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// 取得或設定判斷某個例外是否可重試的函式。
    /// </summary>
    public Func<Exception, bool>? ShouldRetryException { get; set; }

    /// <summary>
    /// 取得或設定判斷某個失敗結果是否可重試的函式。
    /// </summary>
    public Func<FlowFailure, bool>? ShouldRetryFailure { get; set; }
}
