namespace FlowPipeline.Core;

/// <summary>
/// 表示一次失敗的結構化診斷資訊。
/// </summary>
public sealed class FlowFailure
{
    /// <summary>
    /// 初始化 <see cref="FlowFailure"/> 類別的新執行個體。
    /// </summary>
    /// <param name="message">失敗訊息。</param>
    /// <param name="code">選用的失敗代碼。</param>
    /// <param name="payload">選用的失敗承載資料。</param>
    /// <param name="exception">選用的原始例外。</param>
    public FlowFailure(string message, string? code = null, object? payload = null, Exception? exception = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Message = message;
        Code = code;
        Payload = payload;
        Exception = exception;
    }

    /// <summary>
    /// 取得失敗訊息。
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// 取得選用的失敗代碼。
    /// </summary>
    public string? Code { get; }

    /// <summary>
    /// 取得選用的失敗承載資料。
    /// </summary>
    public object? Payload { get; }

    /// <summary>
    /// 取得選用的原始例外。
    /// </summary>
    public Exception? Exception { get; }
}
