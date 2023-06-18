#nullable enable
using Elffy.NativeBind;
using System;

namespace Elffy;

public readonly struct ComputePass  // TODO: make ref sturct
{
    private readonly Screen _screen;
    private readonly Rust.Box<Wgpu.ComputePass> _native;
    private readonly Rust.Box<Wgpu.CommandEncoder> _encoder;

    private ComputePass(Screen screen, Rust.Box<Wgpu.ComputePass> native, Rust.Box<Wgpu.CommandEncoder> encoder)
    {
        _screen = screen;
        _native = native;
        _encoder = encoder;
    }

    private static readonly Action<ComputePass> _release = static self =>
    {
        self._native.DestroyComputePass();
        self._screen.AsRefChecked().FinishCommandEncoder(self._encoder);
    };

    internal static Own<ComputePass> Create(Screen screen)
    {
        var encoder = screen.AsRefChecked().CreateCommandEncoder();
        var native = encoder.AsMut().CreateComputePass();
        return Own.New(new ComputePass(screen, native, encoder), _release);
    }

    public void SetPipeline(ComputePipeline pipeline)
    {
        _native.AsMut().SetPipeline(pipeline.NativeRef);
    }

    public void SetBindGroup(u32 index, BindGroup bindGroup)
    {
        _native.AsMut().SetBindGroup(index, bindGroup.NativeRef);
    }

    public void DispatchWorkgroups(u32 x, u32 y, u32 z)
    {
        _native.AsMut().DispatchWorkgroups(x, y, z);
    }
}
