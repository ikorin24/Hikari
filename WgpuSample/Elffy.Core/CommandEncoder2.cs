#nullable enable
using Elffy.NativeBind;
using System;
using System.ComponentModel;

namespace Elffy;

public readonly ref struct CommandEncoder2
{
    private readonly Rust.OptionBox<Wgpu.CommandEncoder> _encoder;

    internal Rust.MutRef<Wgpu.CommandEncoder> NativeMut => _encoder.Unwrap();

    [Obsolete("Don't use default constructor.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public CommandEncoder2() => throw new NotSupportedException("Don't use default constructor.");

    internal CommandEncoder2(Rust.Box<Wgpu.CommandEncoder> encoder)
    {
        _encoder = encoder;
    }

    internal static CommandEncoder2 Create(Screen screen)
    {
        var encoder = screen.AsRefChecked().CreateCommandEncoder();
        return new CommandEncoder2(encoder);
    }

    internal void Release()
    {
        if(_encoder.IsSome(out var encoder)) {
            encoder.DestroyCommandEncoder();
        }
    }
}

//internal sealed class SurfaceTexture
//{
//    private Rust.OptionBox<Wgpu.SurfaceTexture> _native;
//    private Texture? _surfaceTexture;

//    internal SurfaceTexture()
//    {

//    }
//}
