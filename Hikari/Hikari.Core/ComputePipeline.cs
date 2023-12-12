#nullable enable
using Hikari.NativeBind;
using System;
using System.Collections.Immutable;

namespace Hikari;

public sealed partial class ComputePipeline
{
    private readonly Screen _screen;
    private Rust.OptionBox<Wgpu.ComputePipeline> _native;
    internal Rust.Ref<Wgpu.ComputePipeline> NativeRef => _native.Unwrap();

    public Screen Screen => _screen;

    [Owned(nameof(Release))]
    private ComputePipeline(Screen screen, in ComputePipelineDescriptor desc)
    {
        using(var pin = new PinHandleHolder()) {
            _native = screen.AsRefChecked().CreateComputePipeline(desc.ToNative(pin));
        }
        _screen = screen;
    }

    ~ComputePipeline() => Release(false);

    private void Release()
    {
        Release(true);
        GC.SuppressFinalize(this);
    }

    private void Release(bool disposing)
    {
        if(InterlockedEx.Exchange(ref _native, Rust.OptionBox<Wgpu.ComputePipeline>.None).IsSome(out var native)) {
            native.DestroyComputePipeline();
            if(disposing) {
            }
        }
    }
}

public readonly struct ComputePipelineDescriptor
{
    public PipelineLayout Layout { get; init; }
    public ShaderModule Module { get; init; }
    public required ImmutableArray<byte> EntryPoint { get; init; }


    internal CH.ComputePipelineDescriptor ToNative(PinHandleHolder pins)
    {
        return new CH.ComputePipelineDescriptor
        {
            layout = Layout.NativeRef,
            module = Module.NativeRef,
            entry_point = EntryPoint.AsFixedSlice(pins),
        };
    }
}
