namespace FlowPipeline.Core;

/// <summary>
/// Base class for pipeline errors that provides structured error information.
/// </summary>
public abstract class PipelineError
{
    /// <summary>
    /// Gets or initializes the error message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the error code.
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the timestamp when the error occurred.
    /// </summary>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
