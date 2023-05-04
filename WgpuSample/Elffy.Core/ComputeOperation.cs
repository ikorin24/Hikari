#nullable enable
using Elffy.NativeBind;
using System;

namespace Elffy;

public abstract class ComputeOperation : Operation
{
    private Own<ComputePipeline> _pipeline;
    private ComputePipelineDescriptor _desc;

    protected ComputeOperation(Screen screen, int sortOrder, in ComputePipelineDescriptor desc) : base(screen, sortOrder)
    {
        _desc = desc;
        _pipeline = ComputePipeline.Create(screen, desc);
    }

    protected sealed override void Execute(in CommandEncoder encoder)
    {
        using var pass = ComputePass.Create(encoder.NativeMut);
        Execute(pass.AsValue(), _pipeline.AsValue());
    }

    protected abstract void Execute(in ComputePass pass, ComputePipeline pipeline);
}
