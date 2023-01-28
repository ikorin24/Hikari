#nullable enable
using u32 = System.UInt32;
using i32 = System.Int32;
using Elffy.Bind;
using System.Diagnostics;

namespace Elffy;

internal static class RenderPassExtensions
{
    [DebuggerHidden]
    public static void SetPipeline(
        this MutRef<Wgpu.RenderPass> self,
        Ref<Wgpu.RenderPipeline> renderPipeline)
        => EngineCore.SetPipeline(self, renderPipeline);

    [DebuggerHidden]
    public static void SetBindGroup(
        this MutRef<Wgpu.RenderPass> self,
        u32 index,
        Ref<Wgpu.BindGroup> bindGroup)
        => EngineCore.SetBindGroup(self, index, bindGroup);

    [DebuggerHidden]
    public static void SetVertexBuffer(
        this MutRef<Wgpu.RenderPass> self,
        u32 slot,
        BufferSlice bufferSlice)
        => EngineCore.SetVertexBuffer(self, slot, bufferSlice);

    [DebuggerHidden]
    public static void SetIndexBuffer(this MutRef<Wgpu.RenderPass> self, BufferSlice bufferSlice, wgpu_IndexFormat indexFormat) => EngineCore.SetIndexBuffer(self, bufferSlice, indexFormat);

    [DebuggerHidden]
    public static void DrawIndexed(this MutRef<Wgpu.RenderPass> self, RangeU32 indices, i32 base_vertex, RangeU32 instances) => EngineCore.DrawIndexed(self, indices, base_vertex, instances);
}
