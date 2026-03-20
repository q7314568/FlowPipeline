namespace FlowPipeline.Core;

/// <summary>
/// 表示一次 pipeline 執行的上下文資訊。
/// </summary>
public sealed class PipelineExecutionContext
{
    internal PipelineExecutionContext(Guid executionId, string pipelineName, int totalStages)
    {
        ExecutionId = executionId;
        PipelineName = pipelineName;
        TotalStages = totalStages;
        StartedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// 取得此次執行的唯一識別碼。
    /// </summary>
    public Guid ExecutionId { get; }

    /// <summary>
    /// 取得 pipeline 名稱。
    /// </summary>
    public string PipelineName { get; }

    /// <summary>
    /// 取得此次執行的總階段數。
    /// </summary>
    public int TotalStages { get; }

    /// <summary>
    /// 取得此次執行開始的 UTC 時間。
    /// </summary>
    public DateTimeOffset StartedAtUtc { get; }
}
