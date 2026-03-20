namespace FlowPipeline.Core;

/// <summary>
/// 表示 pipeline 階段的種類。
/// </summary>
public enum PipelineStageKind
{
    /// <summary>
    /// 轉換或驗證步驟。
    /// </summary>
    Step = 0,

    /// <summary>
    /// 不改變值的副作用動作。
    /// </summary>
    Action = 1
}
