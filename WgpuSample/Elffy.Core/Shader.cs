#nullable enable
using System;

namespace Elffy;

public abstract class Shader<TSelf, TMaterial, TOperation>
    : IScreenManaged
    where TSelf : Shader<TSelf, TMaterial, TOperation>
    where TMaterial : Material<TMaterial, TSelf, TOperation>
    where TOperation : RenderOperation<TOperation, TSelf, TMaterial>
{
    private readonly Screen _screen;
    private readonly Own<ShaderModule> _module;
    private readonly Own<RenderPipeline> _pipeline;
    private readonly TOperation _operation;      // TODO: validate
    private bool _released;

    public Screen Screen => _screen;
    public ShaderModule Module => _module.AsValue();
    public TOperation Operation => _operation;
    public RenderPipeline Pipeline => _pipeline.AsValue();

    public bool IsManaged => _released == false;

    protected Shader(
        ReadOnlySpan<byte> shaderSource,
        TOperation operation,
        Func<PipelineLayout, ShaderModule, RenderPipelineDescriptor> getPipelineDesc)
    {
        ArgumentNullException.ThrowIfNull(getPipelineDesc);
        var screen = operation.Screen;
        _screen = screen;
        var module = ShaderModule.Create(screen, shaderSource);
        _module = module;
        var desc = getPipelineDesc.Invoke(operation.PipelineLayout, module.AsValue());
        _pipeline = RenderPipeline.Create(screen, desc);
        _operation = operation;
    }

    private void Release()
    {
        _released = true;
        Release(true);
    }

    protected virtual void Release(bool manualRelease)
    {
        _module.Dispose();
        _pipeline.Dispose();
    }

    protected static Own<TSelf> CreateOwn(TSelf shader)
    {
        ArgumentNullException.ThrowIfNull(shader);
        return Own.New(shader, static x => SafeCast.As<TSelf>(x).Release());
    }

    public virtual void Validate()
    {
        IScreenManaged.DefaultValidate(this);
    }
}
