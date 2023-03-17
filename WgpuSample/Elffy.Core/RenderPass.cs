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

    public static unsafe Own<RenderPass> CreateSurfaceRenderPass(CommandEncoder encoder, SurfaceTextureView surface, TextureView? depth)
    {
        ArgumentNullException.ThrowIfNull(surface);
        var encoderMut = encoder.NativeMut;

        CE.RenderPassDescriptor desc;
        {
            var colorAttachment = new CE.Opt<CE.RenderPassColorAttachment>(new()
            {
                view = surface.Handle.AsRef(),
                clear = new Wgpu.Color(0, 0, 0, 0),
            });
            var depthStencilAttachment = depth switch
            {
                null => CE.Opt<CE.RenderPassDepthStencilAttachment>.None,
                not null => new(new()
                {
                    view = depth.NativeRef,
                    depth_clear = CE.Opt<float>.Some(1f),
                    stencil_clear = CE.Opt<uint>.None,
                }),
            };
            desc = new CE.RenderPassDescriptor
            {
                color_attachments_clear = new() { data = &colorAttachment, len = 1 },
                depth_stencil_attachment_clear = depthStencilAttachment,
            };
        }
        return Create(encoderMut, desc);
    }

    public void SetPipeline(RenderPipeline renderPipeline)
    {
        _native.AsMut().SetPipeline(renderPipeline.NativeRef);
    }
    public void SetBindGroup(u32 index, BindGroup bindGroup)
    {
        _native.AsMut().SetBindGroup(index, bindGroup.NativeRef);
    }
    public void SetVertexBuffer(u32 slot, Buffer buffer)
    {
        var bufferSlice = new CE.BufferSlice(buffer.NativeRef, CE.RangeBoundsU64.RangeFull);
        _native.AsMut().SetVertexBuffer(slot, bufferSlice);
    }
    public void SetIndexBuffer(Buffer buffer, IndexFormat indexFormat)
    {
        var bufferSlice = new CE.BufferSlice(buffer.NativeRef, CE.RangeBoundsU64.RangeFull);
        _native.AsMut().SetIndexBuffer(bufferSlice, indexFormat.MapOrThrow());
    }
    public void DrawIndexed(u32 indexStart, u32 indexCount, i32 baseVertex, u32 instanceStart, u32 instanceCount)
    {
        var indexRange = new CE.RangeU32(indexStart, checked(indexStart + indexCount));
        var instanceRange = new CE.RangeU32(instanceStart, checked(instanceStart + instanceCount));
        _native.AsMut().DrawIndexed(indexRange, baseVertex, instanceRange);
    }
}
