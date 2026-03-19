namespace FlowPipeline.Core;

/// <summary>
/// Pipeline 操作的結果，可以是成功並帶有值，或是失敗並帶有錯誤。
/// </summary>
/// <typeparam name="T">成功值的型別。</typeparam>
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
    /// 取得一個值，指出操作是否成功。
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// 若操作成功，取得成功的值。
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// 若操作失敗，取得錯誤訊息。
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// 若操作失敗，取得錯誤代碼。
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// 若操作失敗，取得詳細的錯誤承載資料。
    /// </summary>
    public PipelineError? ErrorPayload { get; }

    /// <summary>
    /// 建立一個帶有指定值的成功結果。
    /// </summary>
    /// <param name="value">成功的值。</param>
    /// <returns>包含該值的成功 FlowResult。</returns>
    public static FlowResult<T> Success(T value)
    {
        return new FlowResult<T>(true, value, null, null, null);
    }

    /// <summary>
    /// 建立一個帶有指定錯誤訊息和選用錯誤代碼的失敗結果。
    /// </summary>
    /// <param name="message">錯誤訊息。</param>
    /// <param name="errorCode">選用的錯誤代碼。</param>
    /// <returns>包含錯誤資訊的失敗 FlowResult。</returns>
    public static FlowResult<T> Fail(string message, string? errorCode = null)
    {
        return new FlowResult<T>(false, default, message, errorCode, null);
    }

    /// <summary>
    /// 建立一個帶有指定錯誤訊息、錯誤承載資料和選用錯誤代碼的失敗結果。
    /// </summary>
    /// <typeparam name="TError">錯誤承載資料的型別。</typeparam>
    /// <param name="message">錯誤訊息。</param>
    /// <param name="error">錯誤承載資料。</param>
    /// <param name="errorCode">選用的錯誤代碼。</param>
    /// <returns>包含錯誤資訊的失敗 FlowResult。</returns>
    public static FlowResult<T> Fail<TError>(string message, TError error, string? errorCode = null)
        where TError : PipelineError
    {
        return new FlowResult<T>(false, default, message, errorCode, error);
    }

    /// <summary>
    /// 從另一個失敗結果建立相同錯誤資訊的新失敗結果。
    /// </summary>
    /// <typeparam name="TSource">來源結果的值型別。</typeparam>
    /// <param name="source">來源失敗結果。</param>
    /// <returns>包含相同錯誤資訊的失敗 FlowResult。</returns>
    /// <exception cref="ArgumentNullException">當 source 為 null 時。</exception>
    /// <exception cref="InvalidOperationException">當 source 為成功結果時。</exception>
    public static FlowResult<T> FromFailure<TSource>(FlowResult<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.IsSuccess)
        {
            throw new InvalidOperationException("Cannot create a failure result from a successful source result.");
        }

        return new FlowResult<T>(false, default, source.ErrorMessage, source.ErrorCode, source.ErrorPayload);
    }
}
