namespace FlowPipeline.Core;

internal sealed class PipelineExecutionState
{
    public PipelineExecutionState(IServiceProvider? serviceProvider, PipelineExecutionContext execution)
    {
        ServiceProvider = serviceProvider;
        Execution = execution;
    }

    public IServiceProvider? ServiceProvider { get; }

    public PipelineExecutionContext Execution { get; }
}
