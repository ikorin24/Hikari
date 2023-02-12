#nullable enable
using Elffy.Bind;

namespace Elffy;

public readonly struct RenderPass
{
    private readonly Box<Wgpu.RenderPass> _native;

    internal RenderPass(Box<Wgpu.RenderPass> native)
    {
        _native = native;
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
        _native.AsMut().SetVertexBuffer(slot, buffer.NativeRef.AsSlice());
    }
    public void SetIndexBuffer(Buffer buffer, IndexFormat indexFormat)
    {
        _native.AsMut().SetIndexBuffer(buffer.NativeRef.AsSlice(), indexFormat.MapOrThrow());
    }
    public void DrawIndexed(u32 indexStart, u32 indexCount, i32 baseVertex, u32 instanceStart, u32 instanceCount)
    {
        var indexRange = new RangeU32(indexStart, checked(indexStart + indexCount));
        var instanceRange = new RangeU32(instanceStart, checked(instanceStart + instanceCount));
        _native.AsMut().DrawIndexed(indexRange, baseVertex, instanceRange);
    }
}
