#nullable enable

namespace Elffy;

public abstract class ComputeOperation : Operation
{
    private readonly Own<ComputePipeline> _pipeline;

    protected ComputeOperation(Screen screen, int sortOrder, in ComputePipelineDescriptor desc) : base(screen, sortOrder)
    {
        _pipeline = ComputePipeline.Create(screen, desc);
    }

    protected sealed override void Execute(in OperationContext context)
    {
        using var pass = ComputePass.Create(context.Screen);
        Execute(pass.AsValue(), _pipeline.AsValue());
    }

    protected abstract void Execute(in ComputePass pass, ComputePipeline pipeline);
}
