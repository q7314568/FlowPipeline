namespace FlowPipeline.Core;

/// <summary>
/// 表示單一 pipeline 階段嘗試執行時的上下文資訊。
/// </summary>
public sealed class PipelineStageContext
{
    internal PipelineStageContext(
        PipelineExecutionContext execution,
        int stageIndex,
        string stageName,
        PipelineStageKind stageKind,
        Type inputType,
        Type outputType,
        int attempt,
        TimeSpan? timeout)
    {
        Execution = execution;
        StageIndex = stageIndex;
        StageName = stageName;
        StageKind = stageKind;
        InputType = inputType;
        OutputType = outputType;
        Attempt = attempt;
        Timeout = timeout;
    }

    /// <summary>
    /// 取得此次階段所屬的 pipeline 執行上下文。
    /// </summary>
    public PipelineExecutionContext Execution { get; }

    /// <summary>
    /// 取得階段索引，從 1 開始。
    /// </summary>
    public int StageIndex { get; }

    /// <summary>
    /// 取得階段名稱。
    /// </summary>
    public string StageName { get; }

    /// <summary>
    /// 取得階段種類。
    /// </summary>
    public PipelineStageKind StageKind { get; }

    /// <summary>
    /// 取得階段輸入型別。
    /// </summary>
    public Type InputType { get; }

    /// <summary>
    /// 取得階段輸出型別。
    /// </summary>
    public Type OutputType { get; }

    /// <summary>
    /// 取得目前是第幾次嘗試執行。
    /// </summary>
    public int Attempt { get; }

    /// <summary>
    /// 取得此階段設定的逾時值。
    /// </summary>
    public TimeSpan? Timeout { get; }
}
