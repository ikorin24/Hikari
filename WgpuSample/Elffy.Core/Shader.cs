#nullable enable
using System;

namespace Elffy;

public abstract class Shader<TSelf, TMaterial>
    where TSelf : Shader<TSelf, TMaterial>
    where TMaterial : Material<TMaterial, TSelf>
{
    private readonly HostScreen _screen;
    private readonly Own<ShaderModule> _module;
    private readonly Own<BindGroupLayout>[] _bindGroupLayoutOwns;
    private readonly Own<PipelineLayout> _pipelineLayoutOwn;
    private readonly BindGroupLayout[] _bindGroupLayouts;

    public HostScreen Screen => _screen;
    public ShaderModule Module => _module.AsValue();
    public ReadOnlyMemory<BindGroupLayout> BindGroupLayouts => _bindGroupLayouts;
    public PipelineLayout PipelineLayout => _pipelineLayoutOwn.AsValue();

    protected Shader(HostScreen screen, in BindGroupLayoutDescriptor bindGroupLayoutDesc, ReadOnlySpan<byte> shaderSource)
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
        Release(true);
    }

    protected virtual void Release(bool manualRelease)
    {
        _module.Dispose();
        _pipelineLayoutOwn.Dispose();
        foreach(var item in _bindGroupLayoutOwns) {
            item.Dispose();
        }
    }

    protected static Own<TSelf> CreateOwn(TSelf shader)
    {
        ArgumentNullException.ThrowIfNull(shader);
        return Own.RefType(shader, static x => SafeCast.As<TSelf>(x).Release());
    }

    public BindGroupLayout GetBindGroupLayout(int index)
    {
        return _bindGroupLayouts[index];
    }

    //internal Own<Material> CreateMaterial(ReadOnlySpan<BindGroupDescriptor> bindGroupDescs, IDisposable?[]? associates)
    //{
    //    return Material.Create(this, bindGroupDescs, associates);
    //}
}

//public interface IShader<TSelf, TMaterial, TMatArg>
//    where TSelf : Shader, IShader<TSelf, TMaterial, TMatArg>
//    where TMaterial : Material, IMaterial<TMaterial, TSelf, TMatArg>
//{
//    static abstract Own<TSelf> Create(IHostScreen screen);
//}

//public interface IMaterial<TSelf, TShader, TArg>
//    where TSelf : Material, IMaterial<TSelf, TShader, TArg>
//    where TShader : Shader, IShader<TShader, TSelf, TArg>
//{
//    static abstract Own<TSelf> Create(TShader shader, TArg arg);
//}

//public static class ShaderExtensions
//{
//    public static Own<TMaterial> CreateMaterial<TShader, TMaterial, TArg>(this TShader shader, TArg arg)
//        where TShader : Shader, IShader<TShader, TMaterial, TArg>
//        where TMaterial : Material, IMaterial<TMaterial, TShader, TArg>
//    {
//        return TMaterial.Create(shader, arg);
//    }
//}
