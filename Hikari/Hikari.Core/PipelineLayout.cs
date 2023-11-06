#nullable enable
using Hikari.NativeBind;
using System;

namespace Hikari;

public sealed class PipelineLayout : IScreenManaged
{
    private readonly Screen _screen;
    private Rust.OptionBox<Wgpu.PipelineLayout> _native;
    private readonly PipelineLayoutDescriptor _desc;

    public Screen Screen => _screen;

    internal Rust.Ref<Wgpu.PipelineLayout> NativeRef => _native.Unwrap();

    public PipelineLayoutDescriptor Descriptor => _desc;

    public ReadOnlySpan<BindGroupLayout> BindGroupLayouts => _desc.BindGroupLayouts.Span;

    public bool IsManaged => _native.IsNone == false;

    private PipelineLayout(Screen screen, Rust.Box<Wgpu.PipelineLayout> native, in PipelineLayoutDescriptor desc)
    {
        _screen = screen;
        _native = native;
        _desc = desc;
    }

    ~PipelineLayout() => Release(false);

    public void Validate() => IScreenManaged.DefaultValidate(this);

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

    public unsafe static Own<PipelineLayout> Create(Screen screen, in PipelineLayoutDescriptor desc)
    {
        var bindGroupLayouts = desc.BindGroupLayouts.Span;
        var bindGroupLayoutsNative = stackalloc Rust.Ref<Wgpu.BindGroupLayout>[bindGroupLayouts.Length];
        for(int i = 0; i < bindGroupLayouts.Length; i++) {
            bindGroupLayoutsNative[i] = bindGroupLayouts[i].NativeRef;
        }

        var descNative = new CH.PipelineLayoutDescriptor(bindGroupLayoutsNative, (nuint)bindGroupLayouts.Length);
        var pipelineLayoutNative = screen.AsRefChecked().CreatePipelineLayout(descNative);
        var pipelineLayout = new PipelineLayout(screen, pipelineLayoutNative, desc);
        return Own.New(pipelineLayout, static x => SafeCast.As<PipelineLayout>(x).Release());
    }
}

public readonly struct PipelineLayoutDescriptor
{
    public required ReadOnlyMemory<BindGroupLayout> BindGroupLayouts { get; init; }
}
