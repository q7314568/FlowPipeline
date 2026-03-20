namespace FlowPipeline.Core;

/// <summary>
/// 定義 pipeline 執行時的觀測與政策設定。
/// </summary>
public sealed class PipelineOptions
{
    private IReadOnlyList<Abstractions.IPipelineObserver> _observers = Array.Empty<Abstractions.IPipelineObserver>();

    /// <summary>
    /// 取得或設定 pipeline 名稱，供觀測與診斷使用。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 取得或設定每個階段的逾時值。若為 null，則不額外設定逾時。
    /// </summary>
    public TimeSpan? StageTimeout { get; set; }

    /// <summary>
    /// 取得或設定重試設定。若為 null，則不重試。
    /// </summary>
    public PipelineRetryOptions? Retry { get; set; }

    /// <summary>
    /// 取得或設定將例外轉換為結構化失敗的自訂函式。
    /// </summary>
    public Func<PipelineFailureMappingContext, FlowFailure>? FailureMapper { get; set; }

    /// <summary>
    /// 取得或設定要接收 pipeline 執行事件的觀測器集合。
    /// </summary>
    public IReadOnlyList<Abstractions.IPipelineObserver> Observers
    {
        get => _observers;
        set => _observers = value ?? Array.Empty<Abstractions.IPipelineObserver>();
    }
}
