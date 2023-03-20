#nullable enable
using Elffy.NativeBind;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy;

public sealed class SurfaceTextureView : IEngineManaged, ITextureView
{
    private HostScreen? _screen;
    private Rust.OptionBox<Wgpu.TextureView> _native;
    private readonly int _threadId;

    public TextureViewHandle Handle
    {
        get
        {
            CheckThread();
            return new(_native.Expect("Cannot get the native handle at the current timing."));
        }
    }

    public HostScreen? Screen => _screen;

    internal SurfaceTextureView(HostScreen screen, int threadId)
    {
        _screen = screen;
        _native = Rust.OptionBox<Wgpu.TextureView>.None;
        _threadId = threadId;
    }

    internal Rust.OptionBox<Wgpu.TextureView> Replace(Rust.OptionBox<Wgpu.TextureView> value)
    {
        return InterlockedEx.Exchange(ref _native, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckThread()
    {
        if(Environment.CurrentManagedThreadId != _threadId) {
            Throw();

            [DoesNotReturn]
            static void Throw() => throw new InvalidOperationException("Cannot access from the current thread.");
        }
    }
}
