#nullable enable
using Elffy.NativeBind;
using System;
using System.Runtime.CompilerServices;

namespace Elffy;

public readonly struct HostScreenDrawState
{
    private readonly IHostScreen _screen;
    private readonly Rust.Box<Wgpu.CommandEncoder> _commandEncoder;
    private readonly Rust.Box<Wgpu.SurfaceTexture> _surfaceTex;
    private readonly Rust.Box<Wgpu.TextureView> _surfaceView;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal HostScreenDrawState(
        IHostScreen screen,
        Rust.Box<Wgpu.CommandEncoder> encoder,
        Rust.Box<Wgpu.SurfaceTexture> surfaceTex,
        Rust.Box<Wgpu.TextureView> surfaceView)
    {
        _screen = screen;
        _commandEncoder = encoder;
        _surfaceTex = surfaceTex;
        _surfaceView = surfaceView;
    }

    private static readonly Action<HostScreenDrawState> _release = static self =>
    {
        self.Release();
    };

    private void Release()
    {
        _screen.AsRefChecked().ScreenFinishCommand(_commandEncoder, _surfaceTex, _surfaceView);
    }

    internal static bool TryCreate(IHostScreen screen, out Own<HostScreenDrawState> drawState)
    {
        var screenRef = screen.AsRefChecked();
        if(screenRef.ScreenBeginCommand(out var encoder, out var surfaceTex, out var surfaceView) == false) {
            drawState = Own<HostScreenDrawState>.None;
            return false;
        }
        drawState = Own.New(new(screen, encoder, surfaceTex, surfaceView), _release);
        return true;
    }

    public unsafe Own<RenderPass> CreateSurfaceRenderPass()
    {
        CE.RenderPassDescriptor desc;
        {
            var colorAttachment = new Opt<CE.RenderPassColorAttachment>(new()
            {
                view = _surfaceView.AsRef(),
                clear = new Wgpu.Color(0, 0, 0, 0),
            });
            desc = new CE.RenderPassDescriptor
            {
                color_attachments_clear = new() { data = &colorAttachment, len = 1 },
                depth_stencil_attachment_clear = new Opt<CE.RenderPassDepthStencilAttachment>(new()
                {
                    view = _screen.DepthTextureView.NativeRef,
                    depth_clear = Opt<float>.Some(1f),
                    stencil_clear = Opt<uint>.None,
                }),
            };
        }
        return RenderPass.Create(_commandEncoder.AsMut(), desc);
    }

    public unsafe Own<RenderPass> CreateRenderPass(in RenderPassDescriptor desc)
    {
        var colorAttachmentsClearLen = desc.ColorAttachmentsClear.Length;
        var colorAttachmentsClear = stackalloc Opt<CE.RenderPassColorAttachment>[colorAttachmentsClearLen];
        for(int i = 0; i < colorAttachmentsClearLen; i++) {
            colorAttachmentsClear[i] = desc.ColorAttachmentsClear[i] switch
            {
                RenderPassColorAttachment value => new(value.ToNative()),
                null => Opt<CE.RenderPassColorAttachment>.None,
            };
        }

        var descNative = new CE.RenderPassDescriptor
        {
            color_attachments_clear = new() { data = colorAttachmentsClear, len = (usize)colorAttachmentsClearLen },
            depth_stencil_attachment_clear = desc.DepthStencilAttachmentClear switch
            {
                RenderPassDepthStencilAttachment value => new Opt<CE.RenderPassDepthStencilAttachment>(value.ToNative()),
                null => Opt<CE.RenderPassDepthStencilAttachment>.None,
            },
        };

        return RenderPass.Create(_commandEncoder.AsMut(), descNative);
    }
}

public readonly ref struct RenderPassDescriptor
{
    public required ReadOnlySpan<RenderPassColorAttachment?> ColorAttachmentsClear { get; init; }
    public required RenderPassDepthStencilAttachment? DepthStencilAttachmentClear { get; init; }
}

public readonly struct RenderPassColorAttachment
{
    public required TextureView View { get; init; }
    public required (double R, double G, double B, double A) Clear { get; init; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal CE.RenderPassColorAttachment ToNative()
    {
        return new CE.RenderPassColorAttachment
        {
            view = View.NativeRef,
            clear = new Wgpu.Color(Clear.R, Clear.G, Clear.B, Clear.A),
        };
    }
}

public readonly struct RenderPassDepthStencilAttachment
{
    public required TextureView View { get; init; }
    public required f32? DepthClear { get; init; }
    public required u32? StencilClear { get; init; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal CE.RenderPassDepthStencilAttachment ToNative()
    {
        return new CE.RenderPassDepthStencilAttachment
        {
            view = View.NativeRef,
            depth_clear = DepthClear.HasValue ? new Opt<f32>(DepthClear.Value) : Opt<f32>.None,
            stencil_clear = StencilClear.HasValue ? new Opt<u32>(StencilClear.Value) : Opt<u32>.None,
        };
    }
}
