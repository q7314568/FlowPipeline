using FlowPipeline.Core;

namespace FlowPipeline.Abstractions;

/// <summary>
/// 定義可接受額外參數的 Pipeline 步驟介面。
/// </summary>
/// <typeparam name="TIn">輸入型別。</typeparam>
/// <typeparam name="TOut">輸出型別。</typeparam>
/// <typeparam name="TParam">額外參數型別。</typeparam>
public interface IParameterizedPipelineStep<TIn, TOut, TParam>
{
    /// <summary>
    /// 使用額外參數以非同步方式處理輸入並返回結果。
    /// </summary>
    /// <param name="input">輸入值。</param>
    /// <param name="parameter">額外參數。</param>
    /// <param name="ct">取消權杖。</param>
    /// <returns>代表包含結果的非同步操作的工作。</returns>
    Task<FlowResult<TOut>> ProcessAsync(TIn input, TParam parameter, CancellationToken ct = default);
}
