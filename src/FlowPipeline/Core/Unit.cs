namespace FlowPipeline.Core;

/// <summary>
/// Represents a unit type for pipelines that don't require input or output values.
/// </summary>
public readonly struct Unit
{
    /// <summary>
    /// Gets the singleton instance of the Unit type.
    /// </summary>
    public static readonly Unit Value = default;
}
