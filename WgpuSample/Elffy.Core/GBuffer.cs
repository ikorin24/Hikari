#nullable enable
using Elffy.NativeBind;
using System;

namespace Elffy;

internal sealed class GBuffer
{
    private readonly Screen _screen;
    private readonly Own<Texture>[] _colors;
    private readonly CE.Opt<CE.RenderPassColorAttachment>[] _colorsNative;

    public int ColorAttachmentCount => _colors.Length;

    private GBuffer(Screen screen, Vector2u size)
    {
        var colors = new Own<Texture>[2];
        var colorsNative = new CE.Opt<CE.RenderPassColorAttachment>[colors.Length];
        Prepare(screen, size, colors, colorsNative);

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

    public static Own<GBuffer> Create(Screen screen, Vector2u size)
    {
        var gbuffer = new GBuffer(screen, size);
        return Own.RefType(gbuffer, static x => SafeCast.As<GBuffer>(x).Release());
    }

    public Texture ColorAttachment(int index)
    {
        return _colors[index].AsValue();
    }

    public void Resize(Vector2u size)
    {
        foreach(var color in _colors) {
            color.Dispose();
        }
        Prepare(_screen, size, _colors, _colorsNative);
    }

    private static void Prepare(Screen screen, Vector2u size, Span<Own<Texture>> colors, Span<CE.Opt<CE.RenderPassColorAttachment>> colorsNative)
    {
        colors[0] = Texture.Create(screen, new()
        {
            Size = new Vector3u(size.X, size.Y, 1),
            MipLevelCount = 1,
            SampleCount = 1,
            Dimension = TextureDimension.D2,
            Format = TextureFormat.Rgba32Float,
            Usage = TextureUsages.RenderAttachment | TextureUsages.TextureBinding,
        });
        colors[1] = Texture.Create(screen, new()
        {
            Size = new Vector3u(size.X, size.Y, 1),
            MipLevelCount = 1,
            SampleCount = 1,
            Dimension = TextureDimension.D2,
            Format = TextureFormat.Rgba32Float,
            Usage = TextureUsages.RenderAttachment | TextureUsages.TextureBinding,
        });

        for(int i = 0; i < colorsNative.Length; i++) {
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
