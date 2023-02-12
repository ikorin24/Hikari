#nullable enable
using Elffy.NativeBind;
using System;

namespace Elffy;

public sealed class PipelineLayout : IEngineManaged
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

    ~PipelineLayout() => Release(false);

    private static readonly Action<PipelineLayout> _release = static self =>
    {
        self.Release(true);
        GC.SuppressFinalize(self);
    };

    private void Release(bool disposing)
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

    public unsafe static Own<PipelineLayout> Create(IHostScreen screen, in PipelineLayoutDescriptor desc)
    {
        var bindGroupLayouts = desc.BindGroupLayouts.Span;
        var bindGroupLayoutsNative = stackalloc Ref<Wgpu.BindGroupLayout>[bindGroupLayouts.Length];
        for(int i = 0; i < bindGroupLayouts.Length; i++) {
            bindGroupLayoutsNative[i] = bindGroupLayouts[i].NativeRef;
        }

        var descNative = new CE.PipelineLayoutDescriptor(bindGroupLayoutsNative, (nuint)bindGroupLayouts.Length);
        var pipelineLayout = screen.AsRefChecked().CreatePipelineLayout(descNative);
        return Own.New(new PipelineLayout(screen, pipelineLayout), _release);

    }
}

public readonly struct PipelineLayoutDescriptor
{
    public required ReadOnlyMemory<BindGroupLayout> BindGroupLayouts { get; init; }
}
