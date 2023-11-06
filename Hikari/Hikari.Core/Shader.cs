#nullable enable
using System;

namespace Hikari;

public abstract class Shader<TSelf, TMaterial>
    : IScreenManaged
    where TSelf : Shader<TSelf, TMaterial>
    where TMaterial : Material<TMaterial, TSelf>
{
    private readonly Screen _screen;
    private readonly ShaderPass[] _passes;
    private EventSource<TSelf> _disposed;
    private bool _released;

    public Screen Screen => _screen;

    public ReadOnlySpan<ShaderPass> Passes => _passes;
    public Event<TSelf> Disposed => _disposed.Event;

    public bool IsManaged => _released == false;

    protected Shader(Screen screen, in ShaderPassDescriptorArray1 passes)
    {
        if(this is not TSelf) {
            throw new InvalidOperationException("invalid type");
        }
        ArgumentNullException.ThrowIfNull(screen);
        _screen = screen;
        var shaderType = GetType();
        _passes = new ShaderPass[1]
        {
            passes.Pass0.CreateShaderPass(screen, shaderType, 0, Disposed),
        };
        screen.ShaderPasses.Add(_passes);
    }

    protected Shader(Screen screen, in ShaderPassDescriptorArray2 passes)
    {
        if(this is not TSelf) {
            throw new InvalidOperationException("invalid type");
        }
        ArgumentNullException.ThrowIfNull(screen);
        _screen = screen;
        var shaderType = GetType();
        _passes = new ShaderPass[2]
        {
            passes.Pass0.CreateShaderPass(screen, shaderType, 0, Disposed),
            passes.Pass1.CreateShaderPass(screen, shaderType, 1, Disposed),
        };
        screen.ShaderPasses.Add(_passes);
    }

    protected Shader(Screen screen, in ShaderPassDescriptorArray3 passes)
    {
        if(this is not TSelf) {
            throw new InvalidOperationException("invalid type");
        }
        ArgumentNullException.ThrowIfNull(screen);
        _screen = screen;
        var shaderType = GetType();
        _passes = new ShaderPass[3]
        {
            passes.Pass0.CreateShaderPass(screen, shaderType, 0, Disposed),
            passes.Pass1.CreateShaderPass(screen, shaderType, 1, Disposed),
            passes.Pass2.CreateShaderPass(screen, shaderType, 2, Disposed),
        };
        screen.ShaderPasses.Add(_passes);
    }

    protected Shader(Screen screen, in ShaderPassDescriptorArray4 passes)
    {
        if(this is not TSelf) {
            throw new InvalidOperationException("invalid type");
        }
        ArgumentNullException.ThrowIfNull(screen);
        _screen = screen;
        var shaderType = GetType();
        _passes = new ShaderPass[4]
        {
            passes.Pass0.CreateShaderPass(screen, shaderType, 0, Disposed),
            passes.Pass1.CreateShaderPass(screen, shaderType, 1, Disposed),
            passes.Pass2.CreateShaderPass(screen, shaderType, 2, Disposed),
            passes.Pass3.CreateShaderPass(screen, shaderType, 3, Disposed),
        };
        screen.ShaderPasses.Add(_passes);
    }

    private void Release()
    {
        _released = true;
        Release(true);
    }

    protected virtual void Release(bool manualRelease)
    {
        if(manualRelease) {
            _screen.ShaderPasses.Remove(_passes);
            _disposed.Invoke(SafeCast.As<TSelf>(this));
        }
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

public readonly ref struct ShaderPassDescriptor
{
    public required ReadOnlySpan<byte> Source { get; init; }
    public required PipelineLayoutDescriptor LayoutDescriptor { get; init; }
    public required Func<ShaderModule, PipelineLayout, RenderPipelineDescriptor> PipelineDescriptorFactory { get; init; }
    public required int SortOrder { get; init; }
    public required RenderPassFactory RenderPassFactory { get; init; }

    internal ShaderPass CreateShaderPass<_>(Screen screen, Type shaderType, int passIndex, Event<_> lifetimeLimit)
    {
        var module = ShaderModule.Create(screen, Source).DisposeOn(lifetimeLimit);
        var layout = PipelineLayout.Create(screen, LayoutDescriptor).DisposeOn(lifetimeLimit);
        var pipeline = RenderPipeline.Create(screen, PipelineDescriptorFactory(module, layout)).DisposeOn(lifetimeLimit);
        return new ShaderPass(passIndex, this, shaderType, pipeline, layout, module);
    }
}

public readonly record struct RenderPassFactory
{
    public required object? Arg { get; init; }
    public required RenderPassFunc<object?> Factory { get; init; }

    public OwnRenderPass Invoke(Screen screen)
    {
        return Factory(screen, Arg);
    }
}

public readonly ref struct ShaderPassDescriptorArray1
{
    public required ShaderPassDescriptor Pass0 { get; init; }
}

public readonly ref struct ShaderPassDescriptorArray2
{
    public required ShaderPassDescriptor Pass0 { get; init; }
    public required ShaderPassDescriptor Pass1 { get; init; }
}

public readonly ref struct ShaderPassDescriptorArray3
{
    public required ShaderPassDescriptor Pass0 { get; init; }
    public required ShaderPassDescriptor Pass1 { get; init; }
    public required ShaderPassDescriptor Pass2 { get; init; }
}

public readonly ref struct ShaderPassDescriptorArray4
{
    public required ShaderPassDescriptor Pass0 { get; init; }
    public required ShaderPassDescriptor Pass1 { get; init; }
    public required ShaderPassDescriptor Pass2 { get; init; }
    public required ShaderPassDescriptor Pass3 { get; init; }
}
