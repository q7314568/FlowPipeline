using FlowPipeline.Core;

namespace FlowPipeline.Abstractions;

/// <summary>
/// 定義可觀察 pipeline 執行與階段事件的介面。
/// </summary>
public interface IPipelineObserver
{
    /// <summary>
    /// 當一次 pipeline 執行開始時呼叫。
    /// </summary>
    /// <param name="context">此次執行的上下文。</param>
    /// <param name="ct">取消權杖。</param>
    /// <returns>代表非同步操作的工作。</returns>
    ValueTask OnExecutionStartedAsync(PipelineExecutionContext context, CancellationToken ct = default)
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 當一次 pipeline 執行完成時呼叫。
    /// </summary>
    /// <param name="context">此次執行的上下文。</param>
    /// <param name="failure">若執行失敗，則為失敗資訊；成功則為 null。</param>
    /// <param name="ct">取消權杖。</param>
    /// <returns>代表非同步操作的工作。</returns>
    ValueTask OnExecutionCompletedAsync(PipelineExecutionContext context, FlowFailure? failure, CancellationToken ct = default)
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 當某個階段開始執行時呼叫。
    /// </summary>
    /// <param name="context">階段上下文。</param>
    /// <param name="ct">取消權杖。</param>
    /// <returns>代表非同步操作的工作。</returns>
    ValueTask OnStageStartedAsync(PipelineStageContext context, CancellationToken ct = default)
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 當某個階段完成時呼叫。
    /// </summary>
    /// <param name="context">階段上下文。</param>
    /// <param name="failure">若此次階段最終失敗，則為失敗資訊；成功則為 null。</param>
    /// <param name="ct">取消權杖。</param>
    /// <returns>代表非同步操作的工作。</returns>
    ValueTask OnStageCompletedAsync(PipelineStageContext context, FlowFailure? failure, CancellationToken ct = default)
    {
        return ValueTask.CompletedTask;
    }
}
