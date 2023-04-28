#nullable enable
using System;

namespace Elffy;

public abstract class Shader<TSelf, TMaterial>
    : IScreenManaged
    where TSelf : Shader<TSelf, TMaterial>
    where TMaterial : Material<TMaterial, TSelf>
{
    private readonly Screen _screen;
    private readonly Own<ShaderModule> _module;
    private readonly Own<PipelineLayout> _pipelineLayout;
    private bool _released;

    public Screen Screen => _screen;
    public ShaderModule Module => _module.AsValue();
    public PipelineLayout PipelineLayout => _pipelineLayout.AsValue();

    public bool IsManaged => _released == false;

    protected Shader(Screen screen, ReadOnlySpan<byte> shaderSource, in PipelineLayoutDescriptor pipelineLayoutDesc)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _screen = screen;
        _module = ShaderModule.Create(screen, shaderSource);
        _pipelineLayout = PipelineLayout.Create(screen, pipelineLayoutDesc);
    }

    private void Release()
    {
        _released = true;
        Release(true);
    }

    protected virtual void Release(bool manualRelease)
    {
        _module.Dispose();
        _pipelineLayout.Dispose();
    }

    protected static Own<TSelf> CreateOwn(TSelf shader)
    {
        ArgumentNullException.ThrowIfNull(shader);
        return Own.New(shader, static x => SafeCast.As<TSelf>(x).Release());
    }

    public void Validate()
    {
        IScreenManaged.DefaultValidate(this);
    }
}
