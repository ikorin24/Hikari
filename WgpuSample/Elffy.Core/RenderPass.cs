#nullable enable
using Elffy.NativeBind;
using System;

namespace Elffy;

public readonly struct RenderPass
{
    private readonly Rust.Box<Wgpu.RenderPass> _native;

    private RenderPass(Rust.Box<Wgpu.RenderPass> native)
    {
        _native = native;
    }

    private static readonly Action<RenderPass> _release = static self =>
    {
        self._native.DestroyRenderPass();
    };

    internal static Own<RenderPass> Create(Rust.MutRef<Wgpu.CommandEncoder> encoder, in CE.RenderPassDescriptor desc)
    {
        var native = encoder.CreateRenderPass(desc);
        return Own.ValueType(new(native), _release);
    }

    internal static unsafe Own<RenderPass> SurfaceRenderPass(in CommandEncoder encoder)
    {
        var colorAttachment = new CE.Opt<CE.RenderPassColorAttachment>(new()
        {
            view = encoder.SurfaceView,
            clear = new Wgpu.Color(0, 0, 0, 0),
        });
        var desc = new CE.RenderPassDescriptor
        {
            color_attachments_clear = new() { data = &colorAttachment, len = 1 },
            depth_stencil_attachment_clear = new(new()
            {
                view = encoder.Screen.DepthTexture.View.NativeRef,
                depth_clear = CE.Opt<float>.Some(1f),
                stencil_clear = CE.Opt<uint>.None,
            }),
        };
        return Create(encoder.NativeMut, desc);
    }

    public void SetPipeline(RenderPipeline renderPipeline)
    {
        _native.AsMut().SetPipeline(renderPipeline.NativeRef);
    }
    public void SetBindGroup(u32 index, BindGroup bindGroup)
    {
        _native.AsMut().SetBindGroup(index, bindGroup.NativeRef);
    }

    public void SetBindGroups(ReadOnlySpan<BindGroup> bindGroups)
    {
        var native = _native.AsMut();
        for(int i = 0; i < bindGroups.Length; i++) {
            native.SetBindGroup((u32)i, bindGroups[i].NativeRef);
        }
    }

    public void SetVertexBuffer(u32 slot, Buffer buffer)
    {
        var bufferSlice = new CE.BufferSlice(buffer.NativeRef, CE.RangeBoundsU64.RangeFull);
        _native.AsMut().SetVertexBuffer(slot, bufferSlice);
    }
    public void SetVertexBuffer<T>(u32 slot, in BufferSlice<T> bufferSlice) where T : unmanaged
    {
        _native.AsMut().SetVertexBuffer(slot, bufferSlice.Native());
    }

    public void SetIndexBuffer(Buffer buffer, IndexFormat indexFormat)
    {
        var bufferSlice = new CE.BufferSlice(buffer.NativeRef, CE.RangeBoundsU64.RangeFull);
        _native.AsMut().SetIndexBuffer(bufferSlice, indexFormat.MapOrThrow());
    }

    public void SetIndexBuffer(in IndexBufferSlice slice)
    {
        _native.AsMut().SetIndexBuffer(slice.BufferSliceNative(), slice.Format.MapOrThrow());
    }

    public void SetIndexBuffer(in BufferSlice<u32> bufferSlice)
    {
        _native.AsMut().SetIndexBuffer(bufferSlice.Native(), Wgpu.IndexFormat.Uint32);
    }

    public void SetIndexBuffer(in BufferSlice<u16> bufferSlice)
    {
        _native.AsMut().SetIndexBuffer(bufferSlice.Native(), Wgpu.IndexFormat.Uint16);
    }

    public void DrawIndexed(u32 indexStart, u32 indexCount, i32 baseVertex, u32 instanceStart, u32 instanceCount)
    {
        var indexRange = new CE.RangeU32(indexStart, checked(indexStart + indexCount));
        var instanceRange = new CE.RangeU32(instanceStart, checked(instanceStart + instanceCount));
        _native.AsMut().DrawIndexed(indexRange, baseVertex, instanceRange);
    }
}
