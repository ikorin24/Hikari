#nullable enable
using Hikari.NativeBind;
using System;
using System.Threading;

namespace Hikari;

public sealed class GBuffer : IScreenManaged
{
    private readonly Screen _screen;
    private readonly Vector2u _size;
    private Own<Texture>[] _colors;
    private readonly CE.Opt<CE.RenderPassColorAttachment>[] _colorsNative;
    private readonly int _colorAttachmentCount;

    public int ColorAttachmentCount => _colorAttachmentCount;
    public Screen Screen => _screen;
    public bool IsManaged => _colors.Length != 0;
    public Vector2u Size => _size;

    private GBuffer(Screen screen, Vector2u size, ReadOnlySpan<TextureFormat> formats)
    {
        var colors = new Own<Texture>[formats.Length];
        var colorsNative = new CE.Opt<CE.RenderPassColorAttachment>[formats.Length];
        Prepare(screen, size, formats, colors, colorsNative);
        _size = size;
        _colorAttachmentCount = formats.Length;
        _screen = screen;
        _colors = colors;
        _colorsNative = colorsNative;
    }

    private void Release()
    {
        var colors = Interlocked.Exchange(ref _colors, Array.Empty<Own<Texture>>());
        if(colors.Length == 0) {
            return;
        }
        foreach(var color in colors) {
            color.Dispose();
        }
    }

    public void Validate()
    {
        IScreenManaged.DefaultValidate(this);
    }

    public static Own<GBuffer> Create(Screen screen, Vector2u size, ReadOnlySpan<TextureFormat> formats)
    {
        if(formats.IsEmpty) {
            throw new ArgumentException("formats is empty", nameof(formats));
        }

        var gbuffer = new GBuffer(screen, size, formats);
        return Own.New(gbuffer, static x => SafeCast.As<GBuffer>(x).Release());
    }

    public Texture ColorAttachment(int index)
    {
        this.ThrowIfNotScreenManaged();
        return _colors[index].AsValue();
    }

    private static void Prepare(Screen screen, Vector2u size, ReadOnlySpan<TextureFormat> formats, Span<Own<Texture>> colors, Span<CE.Opt<CE.RenderPassColorAttachment>> colorsNative)
    {
        for(int i = 0; i < formats.Length; i++) {
            colors[i] = Texture.Create(screen, new()
            {
                Size = new Vector3u(size.X, size.Y, 1),
                MipLevelCount = 1,
                SampleCount = 1,
                Dimension = TextureDimension.D2,
                Format = formats[i],
                Usage = TextureUsages.RenderAttachment | TextureUsages.TextureBinding | TextureUsages.CopySrc,
            });
            colorsNative[i] = new(new()
            {
                view = colors[i].AsValue().View.NativeRef,
                clear = CE.Opt<Wgpu.Color>.Some(new Wgpu.Color(0, 0, 0, 0)),
            });
        }
    }

    public unsafe OwnRenderPass CreateRenderPass()
    {
        this.ThrowIfNotScreenManaged();
        var screen = Screen;
        var attachmentsNative = _colorsNative;
        fixed(CE.Opt<CE.RenderPassColorAttachment>* p = attachmentsNative) {
            var desc = new CE.RenderPassDescriptor
            {
                color_attachments_clear = new(p, attachmentsNative.Length),
                depth_stencil_attachment_clear = new(new()
                {
                    view = screen.DepthTexture.View.NativeRef,
                    depth_clear = CE.Opt<float>.Some(1f),
                    stencil_clear = CE.Opt<uint>.None,
                }),
            };
            return RenderPass.Create(screen, in desc);
        }
    }
}
