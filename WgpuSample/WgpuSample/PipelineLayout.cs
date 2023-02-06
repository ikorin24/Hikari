#nullable enable
using Elffy.Bind;
using System;
using System.Runtime.InteropServices;

namespace Elffy;

public sealed class PipelineLayout : IEngineManaged, IDisposable
{
    private IHostScreen? _screen;
    private Box<Wgpu.PipelineLayout> _native;

    public IHostScreen? Screen => _screen;

    internal Ref<Wgpu.PipelineLayout> NativeRef => _native;

    private PipelineLayout(IHostScreen screen, Box<Wgpu.PipelineLayout> native)
    {
        _screen = screen;
        _native = native;
    }

    public unsafe static PipelineLayout Create(IHostScreen screen, ReadOnlySpan<BindGroupLayout> layouts)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var screenRef = screen.AsRef();

        var layoutsNative = (Ref<Wgpu.BindGroupLayout>*)NativeMemory.Alloc((usize)layouts.Length * (usize)sizeof(Ref<Wgpu.BindGroupLayout>));
        try {
            for(int i = 0; i < layouts.Length; i++) {
                layoutsNative[i] = layouts[i].NativeRef;
            }
            var desc = new CE.PipelineLayoutDescriptor(layoutsNative, (usize)layouts.Length);
            var pipelineLayout = screenRef.CreatePipelineLayout(desc);
            return new PipelineLayout(screen, pipelineLayout);
        }
        finally {
            NativeMemory.Free(layoutsNative);
        }
    }

    public void Dispose()
    {
        if(_native.IsInvalid) {
            return;
        }
        _native.DestroyPipelineLayout();
        _native = Box<Wgpu.PipelineLayout>.Invalid;
        _screen = null;
    }
}
