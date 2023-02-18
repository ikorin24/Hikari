#nullable enable
using Elffy.NativeBind;
using System;

namespace Elffy;

public sealed class PipelineLayout : IEngineManaged
{
    private IHostScreen? _screen;
    private Rust.OptionBox<Wgpu.PipelineLayout> _native;

    public IHostScreen? Screen => _screen;

    internal Rust.Ref<Wgpu.PipelineLayout> NativeRef => _native.Unwrap();

    private PipelineLayout(IHostScreen screen, Rust.Box<Wgpu.PipelineLayout> native)
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
        if(InterlockedEx.Exchange(ref _native, Rust.OptionBox<Wgpu.PipelineLayout>.None).IsSome(out var native)) {
            native.DestroyPipelineLayout();
            if(disposing) {
                _screen = null;
            }
        }
    }

    public unsafe static Own<PipelineLayout> Create(IHostScreen screen, in PipelineLayoutDescriptor desc)
    {
        var bindGroupLayouts = desc.BindGroupLayouts.Span;
        var bindGroupLayoutsNative = stackalloc Rust.Ref<Wgpu.BindGroupLayout>[bindGroupLayouts.Length];
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
