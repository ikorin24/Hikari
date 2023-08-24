#nullable enable
using Hikari.NativeBind;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Hikari;

public readonly ref struct RenderPass
{
    private readonly Screen _screen;
    private readonly Rust.Box<Wgpu.RenderPass> _native;
    private readonly Rust.Box<Wgpu.CommandEncoder> _encoder;

    [Obsolete("Don't use default constructor.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public RenderPass() => throw new NotSupportedException("Don't use default constructor.");

    private RenderPass(Screen screen, Rust.Box<Wgpu.RenderPass> native, Rust.Box<Wgpu.CommandEncoder> encoder)
    {
        _screen = screen;
        _native = native;
        _encoder = encoder;
    }

    private static readonly ReleaseRenderPass _release = static self =>
    {
        self._native.DestroyRenderPass();
        self._screen.AsRefChecked().FinishCommandEncoder(self._encoder);
    };

    internal static OwnRenderPass Create(Screen screen, scoped in CE.RenderPassDescriptor desc)
    {
        var encoder = screen.AsRefChecked().CreateCommandEncoder();
        var native = encoder.AsMut().CreateRenderPass(desc);
        return new OwnRenderPass(new(screen, native, encoder), _release);
    }

    internal static unsafe OwnRenderPass SurfaceRenderPass(
        Screen screen,
        Rust.Ref<Wgpu.TextureView> surfaceTextureView,
        Rust.Ref<Wgpu.TextureView> depthTextureView,
        (f64 R, f64 G, f64 B, f64 A)? colorClear,
        (f32? DepthClear, u32? StencilClear)? depthStencil)
    {
        var colorAttachment = new CE.Opt<CE.RenderPassColorAttachment>(new()
        {
            view = surfaceTextureView,
            clear = colorClear != null ?
                new(new Wgpu.Color(colorClear.Value.R, colorClear.Value.G, colorClear.Value.B, colorClear.Value.A)) :
                CE.Opt<Wgpu.Color>.None,
        });
        var desc = new CE.RenderPassDescriptor
        {
            color_attachments_clear = new() { data = &colorAttachment, len = 1 },
            depth_stencil_attachment_clear = depthStencil != null ?
                new(new()
                {
                    view = depthTextureView,
                    depth_clear = depthStencil.Value.DepthClear != null ?
                        new(depthStencil.Value.DepthClear.Value) :
                        CE.Opt<float>.None,
                    stencil_clear = depthStencil.Value.StencilClear != null ?
                        new(depthStencil.Value.StencilClear.Value) :
                        CE.Opt<uint>.None,
                }) :
                CE.Opt<CE.RenderPassDepthStencilAttachment>.None,
        };
        return Create(screen, desc);
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
    public void SetVertexBuffer(u32 slot, in BufferSlice bufferSlice)
    {
        _native.AsMut().SetVertexBuffer(slot, bufferSlice.Native());
    }

    public void SetIndexBuffer(Buffer buffer, IndexFormat indexFormat)
    {
        var bufferSlice = new CE.BufferSlice(buffer.NativeRef, CE.RangeBoundsU64.RangeFull);
        _native.AsMut().SetIndexBuffer(bufferSlice, indexFormat.MapOrThrow());
    }

    public void SetIndexBuffer(in BufferSlice bufferSlice, IndexFormat format)
    {
        _native.AsMut().SetIndexBuffer(bufferSlice.Native(), format.MapOrThrow());
    }

    public void SetViewport(
        f32 x,
        f32 y,
        f32 w,
        f32 h,
        f32 minDepth,
        f32 maxDepth)
    {
        _native.AsMut().SetViewport(x, y, w, h, minDepth, maxDepth);
    }

    public void DrawIndexed(u32 indexCount)
    {
        DrawIndexed(0, indexCount, 0, 0, 1);
    }

    public void DrawIndexed(u32 indexStart, u32 indexCount, i32 baseVertex, u32 instanceStart, u32 instanceCount)
    {
        var indexRange = new CE.RangeU32(indexStart, checked(indexStart + indexCount));
        var instanceRange = new CE.RangeU32(instanceStart, checked(instanceStart + instanceCount));
        _native.AsMut().DrawIndexed(indexRange, baseVertex, instanceRange);
    }
}

public readonly ref struct OwnRenderPass
{
    private readonly RenderPass _value;
    internal readonly ReleaseRenderPass? _release;

    [MemberNotNullWhen(false, nameof(_value))]
    [MemberNotNullWhen(false, nameof(_release))]
    public bool IsNone => _release == null;

    public static OwnRenderPass None => default;

    [Obsolete("Don't use default constructor.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public OwnRenderPass() => throw new NotSupportedException("Don't use default constructor.");

    internal OwnRenderPass(RenderPass value, ReleaseRenderPass release)
    {
        ArgumentNullException.ThrowIfNull(release);
        _value = value;
        _release = release;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderPass AsValue()
    {
        if(IsNone) {
            ThrowNoValue();
        }
        return _value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderPass AsValue(out OwnRenderPass self)
    {
        self = this;
        return AsValue();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAsValue(out RenderPass value)
    {
        value = _value;
        return !IsNone;
    }

    public void Dispose()
    {
        if(IsNone) { return; }
        _release.Invoke(_value);
    }

    [DoesNotReturn]
    [DebuggerHidden]
    private static void ThrowNoValue() => throw new InvalidOperationException("no value exists");

    public static explicit operator RenderPass(OwnRenderPass own) => own.AsValue();
}

internal delegate void ReleaseRenderPass(RenderPass renderPass);
