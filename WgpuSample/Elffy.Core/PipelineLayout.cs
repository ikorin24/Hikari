#nullable enable
using Elffy.Bind;
using System;

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

    ~PipelineLayout() => Dispose(false);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        var native = Box.SwapClear(ref _native);
        if(native.IsInvalid) {
            return;
        }
        native.DestroyPipelineLayout();
        if(disposing) {
            _screen = null;
        }
    }

    public unsafe static PipelineLayout Create(IHostScreen screen, in PipelineLayoutDescriptor desc)
    {
        var bindGroupLayouts = desc.BindGroupLayouts.Span;
        var bindGroupLayoutsNative = stackalloc Ref<Wgpu.BindGroupLayout>[bindGroupLayouts.Length];
        for(int i = 0; i < bindGroupLayouts.Length; i++) {
            bindGroupLayoutsNative[i] = bindGroupLayouts[i].NativeRef;
        }

        var descNative = new CE.PipelineLayoutDescriptor(bindGroupLayoutsNative, (nuint)bindGroupLayouts.Length);
        var pipelineLayout = screen.AsRefChecked().CreatePipelineLayout(descNative);
        return new PipelineLayout(screen, pipelineLayout);

    }
}

public readonly struct PipelineLayoutDescriptor
{
    public required ReadOnlyMemory<BindGroupLayout> BindGroupLayouts { get; init; }
}
