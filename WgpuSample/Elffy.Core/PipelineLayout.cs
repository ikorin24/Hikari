#nullable enable
using Elffy.NativeBind;
using System;

namespace Elffy;

public sealed class PipelineLayout : IEngineManaged
{
    private readonly HostScreen _screen;
    private Rust.OptionBox<Wgpu.PipelineLayout> _native;

    public HostScreen Screen => _screen;

    internal Rust.Ref<Wgpu.PipelineLayout> NativeRef => _native.Unwrap();

    public bool IsManaged => _native.IsNone == false;

    private PipelineLayout(HostScreen screen, Rust.Box<Wgpu.PipelineLayout> native)
    {
        _screen = screen;
        _native = native;
    }

    ~PipelineLayout() => Release(false);

    private void Release()
    {
        Release(true);
        GC.SuppressFinalize(this);
    }

    private void Release(bool disposing)
    {
        if(InterlockedEx.Exchange(ref _native, Rust.OptionBox<Wgpu.PipelineLayout>.None).IsSome(out var native)) {
            native.DestroyPipelineLayout();
            if(disposing) {
            }
        }
    }

    public unsafe static Own<PipelineLayout> Create(HostScreen screen, in PipelineLayoutDescriptor desc)
    {
        var bindGroupLayouts = desc.BindGroupLayouts.Span;
        var bindGroupLayoutsNative = stackalloc Rust.Ref<Wgpu.BindGroupLayout>[bindGroupLayouts.Length];
        for(int i = 0; i < bindGroupLayouts.Length; i++) {
            bindGroupLayoutsNative[i] = bindGroupLayouts[i].NativeRef;
        }

        var descNative = new CE.PipelineLayoutDescriptor(bindGroupLayoutsNative, (nuint)bindGroupLayouts.Length);
        var pipelineLayoutNative = screen.AsRefChecked().CreatePipelineLayout(descNative);
        var pipelineLayout = new PipelineLayout(screen, pipelineLayoutNative);
        return Own.RefType(pipelineLayout, static x => SafeCast.As<PipelineLayout>(x).Release());
    }
}

public readonly struct PipelineLayoutDescriptor
{
    public required ReadOnlyMemory<BindGroupLayout> BindGroupLayouts { get; init; }
}
