#nullable enable
using Elffy.NativeBind;
using System;

namespace Elffy;

public sealed class GBuffer
{
    private readonly Screen _screen;
    private Vector2u _size;
    private readonly Own<Texture>[] _colors;
    private readonly CE.Opt<CE.RenderPassColorAttachment>[] _colorsNative;

    public int ColorAttachmentCount => _colors.Length;

    public Screen Screen => _screen;

    private GBuffer(Screen screen, Vector2u size, ReadOnlySpan<TextureFormat> formats)
    {
        var colors = new Own<Texture>[formats.Length];
        var colorsNative = new CE.Opt<CE.RenderPassColorAttachment>[formats.Length];
        Prepare(screen, size, formats, colors, colorsNative);

        _screen = screen;
        _colors = colors;
        _colorsNative = colorsNative;
    }

    private void Release()
    {
        foreach(var color in _colors) {
            color.Dispose();
        }
    }

    public static Own<GBuffer> Create(Screen screen, Vector2u size, ReadOnlySpan<TextureFormat> formats)
    {
        if(formats.IsEmpty) {
            throw new ArgumentException("formats is empty", nameof(formats));
        }

        var gbuffer = new GBuffer(screen, size, formats);
        return Own.RefType(gbuffer, static x => SafeCast.As<GBuffer>(x).Release());
    }

    public Texture ColorAttachment(int index)
    {
        return _colors[index].AsValue();
    }

    public void Resize(Vector2u size)
    {
        if(_size == size) {
            return;
        }
        Span<TextureFormat> formats = stackalloc TextureFormat[_colors.Length];
        for(int i = 0; i < _colors.Length; i++) {
            formats[i] = _colors[i].AsValue().Format;
        }
        foreach(var color in _colors) {
            color.Dispose();
        }
        Prepare(_screen, size, formats, _colors, _colorsNative);
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
                Usage = TextureUsages.RenderAttachment | TextureUsages.TextureBinding,
            });
            colorsNative[i] = new(new()
            {
                view = colors[i].AsValue().View.NativeRef,
                clear = new Wgpu.Color(0, 0, 0, 0),
            });
        }
    }

    public unsafe Own<RenderPass> CreateRenderPass(in CommandEncoder encoder)
    {
        var attachmentsNative = _colorsNative;
        fixed(CE.Opt<CE.RenderPassColorAttachment>* p = attachmentsNative) {
            var desc = new CE.RenderPassDescriptor
            {
                color_attachments_clear = new(p, attachmentsNative.Length),
                depth_stencil_attachment_clear = new(new()
                {
                    view = encoder.Screen.DepthTexture.View.NativeRef,
                    depth_clear = CE.Opt<float>.Some(1f),
                    stencil_clear = CE.Opt<uint>.None,
                }),
            };
            return RenderPass.Create(encoder.NativeMut, in desc);
        }
    }
}
