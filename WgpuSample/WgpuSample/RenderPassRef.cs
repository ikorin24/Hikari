#nullable enable
using u32 = System.UInt32;
using i32 = System.Int32;
using System;
using Elffy.Bind;
using System.ComponentModel;
using System.Diagnostics;

namespace Elffy;

internal readonly ref struct RenderPassRef
{
#pragma warning disable IDE0051
    private readonly IntPtr _p;
#pragma warning restore IDE0051

    [Obsolete("Don't use default constructor.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public RenderPassRef() => throw new NotSupportedException("Don't use default constructor.");

    [DebuggerHidden]
    public void SetPipeline(RenderPipelineHandle renderPipeline) => EngineCore.SetPipeline(this, renderPipeline);

    [DebuggerHidden]
    public void SetBindGroup(u32 index, BindGroupHandle bindGroup) => EngineCore.SetBindGroup(this, index, bindGroup);

    [DebuggerHidden]
    public void SetVertexBuffer(u32 slot, BufSlice bufferSlice) => EngineCore.SetVertexBuffer(this, slot, bufferSlice);

    [DebuggerHidden]
    public void SetIndexBuffer(BufSlice bufferSlice, wgpu_IndexFormat indexFormat) => EngineCore.SetIndexBuffer(this, bufferSlice, indexFormat);

    [DebuggerHidden]
    public void DrawIndexed(RangeU32 indices, i32 base_vertex, RangeU32 instances) => EngineCore.DrawIndexed(this, indices, base_vertex, instances);
}
