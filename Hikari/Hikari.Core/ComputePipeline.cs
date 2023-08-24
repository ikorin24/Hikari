#nullable enable
using Elffy.NativeBind;
using System;

namespace Elffy;

public sealed class ComputePipeline : IScreenManaged
{
    private readonly Screen _screen;
    private Rust.OptionBox<Wgpu.ComputePipeline> _native;
    internal Rust.Ref<Wgpu.ComputePipeline> NativeRef => _native.Unwrap();

    public Screen Screen => _screen;

    public bool IsManaged => _native.IsNone == false;

    private ComputePipeline(Screen screen, Rust.Box<Wgpu.ComputePipeline> native)
    {
        _screen = screen;
        _native = native;
    }

    ~ComputePipeline() => Release(false);

    private static readonly Action<ComputePipeline> _release = static self =>
    {
        self.Release(true);
        GC.SuppressFinalize(self);
    };

    private void Release(bool disposing)
    {
        if(InterlockedEx.Exchange(ref _native, Rust.OptionBox<Wgpu.ComputePipeline>.None).IsSome(out var native)) {
            native.DestroyComputePipeline();
            if(disposing) {
            }
        }
    }

    public void Validate() => IScreenManaged.DefaultValidate(this);


    public static Own<ComputePipeline> Create(Screen screen, in ComputePipelineDescriptor desc)
    {
        Rust.Box<Wgpu.ComputePipeline> native;
        using(var pin = new PinHandleHolder()) {
            native = screen.AsRefChecked().CreateComputePipeline(desc.ToNative(pin));
        }
        var pipeline = new ComputePipeline(screen, native);
        return Own.New(pipeline, static x => _release(SafeCast.As<ComputePipeline>(x)));
    }
}

public readonly struct ComputePipelineDescriptor
{
    public PipelineLayout Layout { get; init; }
    public ShaderModule Module { get; init; }
    public required ReadOnlyMemory<byte> EntryPoint { get; init; }


    internal CE.ComputePipelineDescriptor ToNative(PinHandleHolder pins)
    {
        return new CE.ComputePipelineDescriptor
        {
            layout = Layout.NativeRef,
            module = Module.NativeRef,
            entry_point = EntryPoint.AsFixedSlice(pins),
        };
    }
}
