namespace FlowPipeline.Core;

/// <summary>
/// 表示將例外轉換為 <see cref="FlowFailure"/> 時可使用的上下文資訊。
/// </summary>
public sealed class PipelineFailureMappingContext
{
    internal PipelineFailureMappingContext(
        PipelineStageContext stage,
        Exception exception,
        string defaultMessage,
        string defaultCode,
        bool isTimeout)
    {
        Stage = stage;
        Exception = exception;
        DefaultMessage = defaultMessage;
        DefaultCode = defaultCode;
        IsTimeout = isTimeout;
    }

    /// <summary>
    /// 取得失敗發生的階段資訊。
    /// </summary>
    public PipelineStageContext Stage { get; }

    /// <summary>
    /// 取得原始例外。
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// 取得 FlowPipeline 預設會使用的錯誤訊息。
    /// </summary>
    public string DefaultMessage { get; }

    /// <summary>
    /// 取得 FlowPipeline 預設會使用的錯誤代碼。
    /// </summary>
    public string DefaultCode { get; }

    /// <summary>
    /// 取得此失敗是否由階段逾時所造成。
    /// </summary>
    public bool IsTimeout { get; }
}
