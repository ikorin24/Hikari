#nullable enable
using System;

namespace Elffy;

public sealed class Shader
{
    private readonly IHostScreen _screen;
    private readonly Own<ShaderModule> _module;
    private readonly Own<BindGroupLayout>[] _bindGroupLayoutOwns;
    private readonly Own<PipelineLayout> _pipelineLayoutOwn;
    private readonly BindGroupLayout[] _bindGroupLayouts;

    public IHostScreen Screen => _screen;
    public ShaderModule Module => _module.AsValue();
    public ReadOnlyMemory<BindGroupLayout> BindGroupLayouts => _bindGroupLayouts;
    public PipelineLayout PipelineLayout => _pipelineLayoutOwn.AsValue();

    private Shader(IHostScreen screen, in BindGroupLayoutDescriptor bindGroupLayoutDesc, ReadOnlySpan<byte> shaderSource)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _screen = screen;
        _module = ShaderModule.Create(screen, shaderSource);
        _bindGroupLayoutOwns = new[]
        {
            BindGroupLayout.Create(screen, bindGroupLayoutDesc),
        };
        _bindGroupLayouts = new[]
        {
            _bindGroupLayoutOwns[0].AsValue(),
        };
        _pipelineLayoutOwn = PipelineLayout.Create(screen, new PipelineLayoutDescriptor
        {
            BindGroupLayouts = _bindGroupLayouts,
        });
    }

    private void Release()
    {
        _module.Dispose();
        _pipelineLayoutOwn.Dispose();
        foreach(var item in _bindGroupLayoutOwns) {
            item.Dispose();
        }
    }

    public static Own<Shader> Create(IHostScreen screen, in BindGroupLayoutDescriptor bindGroupLayoutDesc, ReadOnlySpan<byte> shaderSource)
    {
        return new Own<Shader>(new(screen, bindGroupLayoutDesc, shaderSource), static self => self.Release());
    }

    public BindGroupLayout GetBindGroupLayout(int index)
    {
        return _bindGroupLayouts[index];
    }

    internal Own<Material> CreateMaterial(ReadOnlySpan<BindGroupDescriptor> bindGroupDescs)
    {
        return Material.Create(this, bindGroupDescs);
    }
}
