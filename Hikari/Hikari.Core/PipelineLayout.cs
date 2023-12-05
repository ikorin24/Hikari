#nullable enable
using Hikari.NativeBind;
using System;
using System.Collections.Immutable;

namespace Hikari;

public sealed partial class PipelineLayout : IScreenManaged
{
    private readonly Screen _screen;
    private Rust.OptionBox<Wgpu.PipelineLayout> _native;
    private readonly PipelineLayoutDescriptor _desc;

    public Screen Screen => _screen;

    internal Rust.Ref<Wgpu.PipelineLayout> NativeRef => _native.Unwrap();

    public PipelineLayoutDescriptor Descriptor => _desc;

    public ReadOnlySpan<BindGroupLayout> BindGroupLayouts => _desc.BindGroupLayouts.AsSpan();

    public bool IsManaged => _native.IsNone == false;

    [Owned(nameof(Release))]
    private PipelineLayout(Screen screen, in PipelineLayoutDescriptor desc)
    {
        unsafe {
            var bindGroupLayouts = desc.BindGroupLayouts.AsSpan();
            var bindGroupLayoutsNative = stackalloc Rust.Ref<Wgpu.BindGroupLayout>[bindGroupLayouts.Length];
            for(int i = 0; i < bindGroupLayouts.Length; i++) {
                bindGroupLayoutsNative[i] = bindGroupLayouts[i].NativeRef;
            }
            var descNative = new CH.PipelineLayoutDescriptor(bindGroupLayoutsNative, (nuint)bindGroupLayouts.Length);
            _native = screen.AsRefChecked().CreatePipelineLayout(descNative);
        }
        _screen = screen;
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
}

public readonly struct PipelineLayoutDescriptor
{
    public required ImmutableArray<BindGroupLayout> BindGroupLayouts { get; init; }
}
