#nullable enable
using Elffy.NativeBind;
using System;

namespace Elffy;

// an instance cannot move to another thread
public readonly struct ComputePass
{
    private readonly Rust.Box<Wgpu.ComputePass> _native;

    private ComputePass(Rust.Box<Wgpu.ComputePass> native)
    {
        _native = native;
    }

    private static readonly Action<ComputePass> _release = static self =>
    {
        self._native.DestroyComputePass();
    };

    internal static Own<ComputePass> Create(Rust.MutRef<Wgpu.CommandEncoder> encoder)
    {
        var native = encoder.CreateComputePass();
        return Own.New(new ComputePass(native), _release);
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
