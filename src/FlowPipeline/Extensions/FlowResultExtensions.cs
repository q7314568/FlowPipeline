using FlowPipeline.Core;

namespace FlowPipeline.Extensions;

/// <summary>
/// Extension methods for FlowResult.
/// </summary>
public static class FlowResultExtensions
{
    /// <summary>
    /// Tries to get the error payload as a specific error type.
    /// </summary>
    /// <typeparam name="T">The value type of the FlowResult.</typeparam>
    /// <typeparam name="TError">The error type to cast to.</typeparam>
    /// <param name="result">The FlowResult to extract the error from.</param>
    /// <param name="error">The error payload if successful, otherwise null.</param>
    /// <returns>True if the error was successfully extracted and cast, otherwise false.</returns>
    public static bool TryGetError<T, TError>(this FlowResult<T> result, out TError? error)
        where TError : class
    {
        error = result.ErrorPayload as TError;
        return error != null;
    }

    /// <summary>
    /// Gets the error payload as a specific error type.
    /// </summary>
    /// <typeparam name="T">The value type of the FlowResult.</typeparam>
    /// <typeparam name="TError">The error type to cast to.</typeparam>
    /// <param name="result">The FlowResult to extract the error from.</param>
    /// <returns>The error payload cast to the specified type, or null if not available or cannot be cast.</returns>
    public static TError? GetErrorAs<T, TError>(this FlowResult<T> result)
        where TError : class
    {
        return result.ErrorPayload as TError;
    }
}
