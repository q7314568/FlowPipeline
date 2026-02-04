using FlowPipeline.Core;

namespace FlowPipeline.Abstractions;

/// <summary>
/// Represents a pipeline step that transforms input of type TIn to output of type TOut.
/// </summary>
/// <typeparam name="TIn">The input type.</typeparam>
/// <typeparam name="TOut">The output type.</typeparam>
public interface IPipelineStep<TIn, TOut>
{
    /// <summary>
    /// Processes the input asynchronously and returns a result.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the result.</returns>
    Task<FlowResult<TOut>> ProcessAsync(TIn input, CancellationToken ct = default);
}
