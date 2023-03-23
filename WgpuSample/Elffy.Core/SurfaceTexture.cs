#nullable enable
using Elffy.NativeBind;

namespace Elffy;

public sealed class SurfaceTextureView : IEngineManaged, ITextureView
{
    private readonly HostScreen _screen;
    private Rust.OptionBox<Wgpu.TextureView> _native;
    private readonly ThreadId _threadId;

    public TextureViewHandle Handle
    {
        get
        {
            _threadId.ThrowIfNotMatched();
            return new(_native.Expect("Cannot get the native handle at the current timing."));
        }
    }

    public HostScreen Screen => _screen;

    public bool IsManaged => _native.IsNone == false;

    internal SurfaceTextureView(HostScreen screen, ThreadId threadId)
    {
        _screen = screen;
        _native = Rust.OptionBox<Wgpu.TextureView>.None;
        _threadId = threadId;
    }

    internal Rust.OptionBox<Wgpu.TextureView> Replace(Rust.OptionBox<Wgpu.TextureView> value)
    {
        return InterlockedEx.Exchange(ref _native, value);
    }
}
