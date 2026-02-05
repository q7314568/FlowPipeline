using FlowPipeline.Core;

namespace FlowPipeline.Extensions;

/// <summary>
/// FlowResult 的擴充方法。
/// </summary>
public static class FlowResultExtensions
{
    /// <summary>
    /// 嘗試將錯誤承載資料取得為特定的錯誤型別。
    /// </summary>
    /// <typeparam name="T">FlowResult 的值型別。</typeparam>
    /// <typeparam name="TError">要轉換成的錯誤型別。</typeparam>
    /// <param name="result">要從中提取錯誤的 FlowResult。</param>
    /// <param name="error">若成功則為錯誤承載資料，否則為 null。</param>
    /// <returns>若錯誤成功提取並轉換則為 true，否則為 false。</returns>
    public static bool TryGetError<T, TError>(this FlowResult<T> result, out TError? error)
        where TError : class
    {
        error = result.ErrorPayload as TError;
        return error != null;
    }

    /// <summary>
    /// 取得錯誤承載資料作為特定的錯誤型別。
    /// </summary>
    /// <typeparam name="T">FlowResult 的值型別。</typeparam>
    /// <typeparam name="TError">要轉換成的錯誤型別。</typeparam>
    /// <param name="result">要從中提取錯誤的 FlowResult。</param>
    /// <returns>轉換為指定型別的錯誤承載資料，若無法取得或無法轉換則為 null。</returns>
    public static TError? GetErrorAs<T, TError>(this FlowResult<T> result)
        where TError : class
    {
        return result.ErrorPayload as TError;
    }
}
