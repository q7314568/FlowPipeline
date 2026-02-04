namespace FlowPipeline.Core;

/// <summary>
/// Represents the result of a pipeline operation that can either succeed with a value or fail with an error.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public class FlowResult<T>
{
    private FlowResult(bool isSuccess, T? value, string? errorMessage, string? errorCode, PipelineError? errorPayload)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
        ErrorPayload = errorPayload;
    }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the success value if the operation succeeded.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the error code if the operation failed.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Gets the detailed error payload if the operation failed.
    /// </summary>
    public PipelineError? ErrorPayload { get; }

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <param name="value">The success value.</param>
    /// <returns>A successful FlowResult containing the value.</returns>
    public static FlowResult<T> Success(T value)
    {
        return new FlowResult<T>(true, value, null, null, null);
    }

    /// <summary>
    /// Creates a failed result with the specified error message and optional error code.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorCode">The optional error code.</param>
    /// <returns>A failed FlowResult containing the error information.</returns>
    public static FlowResult<T> Fail(string message, string? errorCode = null)
    {
        return new FlowResult<T>(false, default, message, errorCode, null);
    }

    /// <summary>
    /// Creates a failed result with the specified error message, error payload, and optional error code.
    /// </summary>
    /// <typeparam name="TError">The type of the error payload.</typeparam>
    /// <param name="message">The error message.</param>
    /// <param name="error">The error payload.</param>
    /// <param name="errorCode">The optional error code.</param>
    /// <returns>A failed FlowResult containing the error information.</returns>
    public static FlowResult<T> Fail<TError>(string message, TError error, string? errorCode = null)
        where TError : PipelineError
    {
        return new FlowResult<T>(false, default, message, errorCode, error);
    }
}
