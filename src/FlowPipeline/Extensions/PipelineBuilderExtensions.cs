using FlowPipeline.Core;

namespace FlowPipeline.Extensions;

/// <summary>
/// Extension methods for PipelineBuilder.
/// </summary>
public static class PipelineBuilderExtensions
{
    /// <summary>
    /// Maps the current pipeline value using a transformation function.
    /// </summary>
    /// <typeparam name="T">The type of the value to transform.</typeparam>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="transform">The transformation function.</param>
    /// <returns>A new PipelineBuilder with the transformed value.</returns>
    public static PipelineBuilder<T> Map<T>(this PipelineBuilder<T> builder, Func<T, T> transform)
    {
        return builder.Then(async (input, ct) =>
        {
            await Task.CompletedTask;
            var transformed = transform(input);
            return FlowResult<T>.Success(transformed);
        });
    }
}
