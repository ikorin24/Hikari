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

    internal static void ClearSurface(Screen screen, (f64 R, f64 G, f64 B, f64 A) clearColor)
    {
        using var pass = Create(
            screen,
            new ColorAttachment
            {
                Target = screen.Surface,
                LoadOp = ColorBufferInit.Clear(clearColor),
            },
            null);
    }

    internal static OwnRenderPass Create(Screen screen, scoped in CH.RenderPassDescriptor desc)
    {
        var encoder = screen.AsRefChecked().CreateCommandEncoder();
        var native = encoder.AsMut().CreateRenderPass(desc);
        return new OwnRenderPass(new(screen, native, encoder), _release);
    }

    public static unsafe OwnRenderPass Create(Screen screen, scoped in ColorAttachment? color, scoped in DepthStencilAttachment? depthStencil)
    {
        var colorsNative = color switch
        {
            ColorAttachment c => new(c.ToNative()),
            null => CH.Opt<CH.RenderPassColorAttachment>.None,
        };
        var desc = new CH.RenderPassDescriptor
        {
            color_attachments = new() { data = &colorsNative, len = 1 },
            depth_stencil_attachment = depthStencil switch
            {
                DepthStencilAttachment ds => new(ds.ToNative()),
                null => CH.Opt<CH.RenderPassDepthStencilAttachment>.None,
            },
        };
        return Create(screen, desc);
    }

    public static unsafe OwnRenderPass Create(Screen screen, scoped ReadOnlySpan<ColorAttachment?> colors, scoped in DepthStencilAttachment? depthStencil)
    {
        var colorsNative = stackalloc CH.Opt<CH.RenderPassColorAttachment>[colors.Length];
        for(int i = 0; i < colors.Length; i++) {
            colorsNative[i] = colors[i] switch
            {
                ColorAttachment color => new(color.ToNative()),
                null => CH.Opt<CH.RenderPassColorAttachment>.None,
            };
        }

        var desc = new CH.RenderPassDescriptor
        {
            color_attachments = new() { data = colorsNative, len = (u32)colors.Length },
            depth_stencil_attachment = depthStencil switch
            {
                DepthStencilAttachment ds => new(ds.ToNative()),
                null => CH.Opt<CH.RenderPassDepthStencilAttachment>.None,
            },
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

public delegate OwnRenderPass RenderPassFunc<T>(T arg);

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

public readonly record struct ColorAttachment
{
    public required ITextureView Target { get; init; }
    public required ColorBufferInit LoadOp { get; init; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal CH.RenderPassColorAttachment ToNative()
    {
        return new()
        {
            view = Target.ViewNativeRef,
            init = LoadOp.ToNative(),
        };
    }
}

public readonly record struct DepthStencilAttachment
{
    public required ITextureView Target { get; init; }
    public required DepthStencilBufferInit LoadOp { get; init; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal CH.RenderPassDepthStencilAttachment ToNative()
    {
        return new()
        {
            view = Target.ViewNativeRef,
            depth = LoadOp.Depth switch
            {
                DepthBufferInit depth => new(depth.ToNative()),
                null => CH.Opt<CH.RenderPassDepthBufferInit>.None,
            },
            stencil = LoadOp.Stencil switch
            {
                StencilBufferInit stencil => new(stencil.ToNative()),
                null => CH.Opt<CH.RenderPassStencilBufferInit>.None,
            },
        };
    }
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

    public static ColorBufferInit Clear((f64 R, f64 G, f64 B, f64 A) clearValue)
    {
        return new ColorBufferInit
        {
            Mode = RenderPassInitMode.Clear,
            ClearValue = clearValue,
        };
    }

    public static ColorBufferInit Load()
    {
        return new ColorBufferInit
        {
            Mode = RenderPassInitMode.Load,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal CH.RenderPassColorBufferInit ToNative()
    {
        return new()
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
}

public enum RenderPassInitMode : u32
{
    Clear = 0,
    Load = 1,
}

internal delegate void ReleaseRenderPass(RenderPass renderPass);
