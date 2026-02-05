namespace FlowPipeline.Core;

/// <summary>
/// 表示不需要輸入或輸出值的 Pipeline 所使用的單元型別。
/// </summary>
public readonly struct Unit
{
    /// <summary>
    /// 取得 Unit 型別的單例實例。
    /// </summary>
    public static readonly Unit Value = default;
}
