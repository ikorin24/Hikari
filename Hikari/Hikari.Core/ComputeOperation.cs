#nullable enable

namespace Hikari;

public abstract class ComputeOperation : Operation<ComputeOperation>
{
    private readonly Own<ComputePipeline> _pipeline;

    private protected ComputeOperation(Screen screen, in ComputePipelineDescriptor desc) : base(screen)
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
