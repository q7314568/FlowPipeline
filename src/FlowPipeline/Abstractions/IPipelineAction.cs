namespace FlowPipeline.Abstractions;

/// <summary>
/// Represents a pipeline action that performs a side effect with input of type TIn.
/// </summary>
/// <typeparam name="TIn">The input type.</typeparam>
public interface IPipelineAction<TIn>
{
    /// <summary>
    /// Executes the action asynchronously with the provided input.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteAsync(TIn input, CancellationToken ct = default);
}

/// <summary>
/// Represents a pipeline action that performs a side effect without input.
/// </summary>
public interface IPipelineAction
{
    /// <summary>
    /// Executes the action asynchronously.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteAsync(CancellationToken ct = default);
}
