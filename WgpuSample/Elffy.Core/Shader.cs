#nullable enable
using System;

namespace Elffy;

public abstract class Shader<TSelf, TMaterial>
    where TSelf : Shader<TSelf, TMaterial>
    where TMaterial : Material<TMaterial, TSelf>
{
    private readonly Screen _screen;
    private readonly Own<ShaderModule> _module;
    private readonly Own<PipelineLayout> _pipelineLayout;

    public Screen Screen => _screen;
    public ShaderModule Module => _module.AsValue();
    public PipelineLayout PipelineLayout => _pipelineLayout.AsValue();

    protected Shader(Screen screen, ReadOnlySpan<byte> shaderSource, in PipelineLayoutDescriptor pipelineLayoutDesc)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _screen = screen;
        _module = ShaderModule.Create(screen, shaderSource);
        _pipelineLayout = PipelineLayout.Create(screen, pipelineLayoutDesc);
    }

    private void Release()
    {
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
        return Own.RefType(shader, static x => SafeCast.As<TSelf>(x).Release());
    }
}
