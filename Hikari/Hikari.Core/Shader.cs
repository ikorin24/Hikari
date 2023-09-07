#nullable enable
using System;

namespace Hikari;

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
        Own<RenderPipeline> pipeline)
    {
        ArgumentNullException.ThrowIfNull(operation);
        pipeline.ThrowArgumentExceptionIfNone();
        _screen = operation.Screen;
        _module = ShaderModule.Create(_screen, shaderSource);
        _pipeline = pipeline;
        _operation = operation;
    }

    protected Shader(
        ReadOnlySpan<byte> shaderSource,
        TOperation operation,
        Func<TOperation, ShaderModule, RenderPipelineDescriptor> getPipelineDesc)
    {
        ArgumentNullException.ThrowIfNull(getPipelineDesc);
        ArgumentNullException.ThrowIfNull(operation);
        _screen = operation.Screen;
        _module = ShaderModule.Create(_screen, shaderSource);
        var desc = getPipelineDesc.Invoke(operation, _module.AsValue());
        _pipeline = RenderPipeline.Create(_screen, desc);
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

    protected static Own<T> CreateOwn<T>(T shader) where T : TSelf
    {
        ArgumentNullException.ThrowIfNull(shader);
        return Own.New(shader, static x => SafeCast.As<TSelf>(x).Release());
    }

    public virtual void Validate()
    {
        IScreenManaged.DefaultValidate(this);
    }
}
