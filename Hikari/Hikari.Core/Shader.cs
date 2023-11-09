#nullable enable
using System;

namespace Hikari;

public abstract class Shader : IScreenManaged
{
    private readonly Screen _screen;
    private bool _released;
    public bool IsManaged => _released == false;

    public Screen Screen => _screen;

    public abstract ReadOnlySpan<ShaderPass> Passes { get; }

    protected private Shader(Screen screen)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _screen = screen;
    }

    private void Release()
    {
        _released = true;
        Release(true);
    }

    protected virtual void Release(bool manualRelease)
    {
    }

    protected static Own<T> CreateOwn<T>(T shader) where T : Shader
    {
        ArgumentNullException.ThrowIfNull(shader);
        return Own.New(shader, static x => SafeCast.As<Shader>(x).Release());
    }

    public virtual void Validate()
    {
        IScreenManaged.DefaultValidate(this);
    }
}

public abstract class Shader<TSelf, TMaterial>
    : Shader
    where TSelf : Shader<TSelf, TMaterial>
    where TMaterial : Material<TMaterial, TSelf>
{
    private readonly ShaderPass[] _passes;
    private EventSource<TSelf> _disposed;

    public sealed override ReadOnlySpan<ShaderPass> Passes => _passes;
    public Event<TSelf> Disposed => _disposed.Event;

    protected Shader(Screen screen, in ShaderPassDescriptorArray1 passes) : base(screen)
    {
        if(this is not TSelf) {
            throw new InvalidOperationException("invalid type");
        }
        _passes = new ShaderPass[1]
        {
            passes.Pass0.CreateShaderPass(this, 0, Disposed),
        };
        screen.ShaderPasses.Add(_passes);
    }

    protected Shader(Screen screen, in ShaderPassDescriptorArray2 passes) : base(screen)
    {
        if(this is not TSelf) {
            throw new InvalidOperationException("invalid type");
        }
        ArgumentNullException.ThrowIfNull(screen);
        _passes = new ShaderPass[2]
        {
            passes.Pass0.CreateShaderPass(this, 0, Disposed),
            passes.Pass1.CreateShaderPass(this, 1, Disposed),
        };
        screen.ShaderPasses.Add(_passes);
    }

    protected Shader(Screen screen, in ShaderPassDescriptorArray3 passes) : base(screen)
    {
        if(this is not TSelf) {
            throw new InvalidOperationException("invalid type");
        }
        ArgumentNullException.ThrowIfNull(screen);
        _passes = new ShaderPass[3]
        {
            passes.Pass0.CreateShaderPass(this, 0, Disposed),
            passes.Pass1.CreateShaderPass(this, 1, Disposed),
            passes.Pass2.CreateShaderPass(this, 2, Disposed),
        };
        screen.ShaderPasses.Add(_passes);
    }

    protected Shader(Screen screen, in ShaderPassDescriptorArray4 passes) : base(screen)
    {
        if(this is not TSelf) {
            throw new InvalidOperationException("invalid type");
        }
        ArgumentNullException.ThrowIfNull(screen);
        _passes = new ShaderPass[4]
        {
            passes.Pass0.CreateShaderPass(this, 0, Disposed),
            passes.Pass1.CreateShaderPass(this, 1, Disposed),
            passes.Pass2.CreateShaderPass(this, 2, Disposed),
            passes.Pass3.CreateShaderPass(this, 3, Disposed),
        };
        screen.ShaderPasses.Add(_passes);
    }

    protected override void Release(bool manualRelease)
    {
        base.Release(manualRelease);
        if(manualRelease) {
            Screen.ShaderPasses.Remove(_passes);
            _disposed.Invoke(SafeCast.As<TSelf>(this));
        }
    }
}
