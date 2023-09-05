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

    internal static OwnRenderPass Create(Screen screen, scoped in CH.RenderPassDescriptor desc)
    {
        var encoder = screen.AsRefChecked().CreateCommandEncoder();
        var native = encoder.AsMut().CreateRenderPass(desc);
        return new OwnRenderPass(new(screen, native, encoder), _release);
    }

    public static unsafe OwnRenderPass Create(
        Screen screen,
        ITextureView surfaceTextureView,
        scoped in ColorBufferInit colorInit)
    {
        var color = new CH.Opt<CH.RenderPassColorAttachment>(colorInit.ToNative(surfaceTextureView.ViewNativeRef));
        var desc = new CH.RenderPassDescriptor
        {
            color_attachments = new() { data = &color, len = 1 },
            depth_stencil_attachment = CH.Opt<CH.RenderPassDepthStencilAttachment>.None,
        };
        return Create(screen, desc);
    }

    public static unsafe OwnRenderPass Create(
        Screen screen,
        ITextureView surfaceTextureView,
        ITextureView depthStencilTextureView,
        scoped in ColorBufferInit colorInit,
        scoped in DepthStencilBufferInit depthStencilInit)
    {
        var color = new CH.Opt<CH.RenderPassColorAttachment>(colorInit.ToNative(surfaceTextureView.ViewNativeRef));
        var desc = new CH.RenderPassDescriptor
        {
            color_attachments = new() { data = &color, len = 1 },
            depth_stencil_attachment = new(depthStencilInit.ToNative(depthStencilTextureView.ViewNativeRef)),
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
        var bufferSlice = new CH.BufferSlice(buffer.NativeRef, CH.RangeBoundsU64.RangeFull);
        _native.AsMut().SetVertexBuffer(slot, bufferSlice);
    }
    public void SetVertexBuffer(u32 slot, in BufferSlice bufferSlice)
    {
        _native.AsMut().SetVertexBuffer(slot, bufferSlice.Native());
    }

    public void SetIndexBuffer(Buffer buffer, IndexFormat indexFormat)
    {
        var bufferSlice = new CH.BufferSlice(buffer.NativeRef, CH.RangeBoundsU64.RangeFull);
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
        var indexRange = new CH.RangeU32(indexStart, checked(indexStart + indexCount));
        var instanceRange = new CH.RangeU32(instanceStart, checked(instanceStart + instanceCount));
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

public readonly record struct ColorBufferInit
{
    public required RenderPassInitMode Mode { get; init; }
    public (f64 R, f64 G, f64 B, f64 A) ClearValue { get; init; }

    public static ColorBufferInit Clear()
    {
        return new ColorBufferInit
        {
            Mode = RenderPassInitMode.Clear,
            ClearValue = (0, 0, 0, 0),
        };
    }

    public static ColorBufferInit Clear(f64 r, f64 g, f64 b, f64 a)
    {
        return new ColorBufferInit
        {
            Mode = RenderPassInitMode.Clear,
            ClearValue = (r, g, b, a),
        };
    }

    public static ColorBufferInit Load()
    {
        return new ColorBufferInit
        {
            Mode = RenderPassInitMode.Load,
        };
    }

    internal CH.RenderPassColorAttachment ToNative(Rust.Ref<Wgpu.TextureView> view)
    {
        return new()
        {
            view = view,
            init = new()
            {
                mode = Mode switch
                {
                    RenderPassInitMode.Clear => CH.RenderPassBufferInitMode.Clear,
                    RenderPassInitMode.Load or _ => CH.RenderPassBufferInitMode.Load,
                },
                value = new Wgpu.Color
                {
                    R = ClearValue.R,
                    G = ClearValue.G,
                    B = ClearValue.B,
                    A = ClearValue.A,
                },
            },
        };
    }
}

public readonly record struct DepthBufferInit
{
    public required RenderPassInitMode Mode { get; init; }
    public f32 ClearValue { get; init; }

    public static DepthBufferInit Clear(f32 clearValue) => new DepthBufferInit
    {
        Mode = RenderPassInitMode.Clear,
        ClearValue = clearValue,
    };

    public static DepthBufferInit Load() => new DepthBufferInit
    {
        Mode = RenderPassInitMode.Load,
    };

    internal CH.RenderPassDepthBufferInit ToNative()
    {
        return new()
        {
            mode = Mode switch
            {
                RenderPassInitMode.Clear => CH.RenderPassBufferInitMode.Clear,
                RenderPassInitMode.Load or _ => CH.RenderPassBufferInitMode.Load,
            },
            value = ClearValue,
        };
    }
}

public readonly record struct StencilBufferInit
{
    public required RenderPassInitMode Mode { get; init; }
    public u32 ClearValue { get; init; }

    public static StencilBufferInit Clear(u32 clearValue) => new StencilBufferInit
    {
        Mode = RenderPassInitMode.Clear,
        ClearValue = clearValue,
    };

    public static StencilBufferInit Load() => new StencilBufferInit
    {
        Mode = RenderPassInitMode.Load,
    };

    internal CH.RenderPassStencilBufferInit ToNative()
    {
        return new()
        {
            mode = Mode switch
            {
                RenderPassInitMode.Clear => CH.RenderPassBufferInitMode.Clear,
                RenderPassInitMode.Load or _ => CH.RenderPassBufferInitMode.Load,
            },
            value = ClearValue,
        };
    }
}

public readonly record struct DepthStencilBufferInit
{
    public required DepthBufferInit? Depth { get; init; }
    public required StencilBufferInit? Stencil { get; init; }

    internal CH.RenderPassDepthStencilAttachment ToNative(Rust.Ref<Wgpu.TextureView> view)
    {
        return new()
        {
            view = view,
            depth = Depth switch
            {
                null => CH.Opt<CH.RenderPassDepthBufferInit>.None,
                DepthBufferInit depthInit => new(depthInit.ToNative()),
            },
            stencil = Stencil switch
            {
                null => CH.Opt<CH.RenderPassStencilBufferInit>.None,
                StencilBufferInit stencilInit => new(stencilInit.ToNative()),
            },
        };
    }
}

public enum RenderPassInitMode : u32
{
    Clear = 0,
    Load = 1,
}

internal delegate void ReleaseRenderPass(RenderPass renderPass);
