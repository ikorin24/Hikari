#nullable enable
using Hikari.NativeBind;
using System;
using System.Threading;

namespace Hikari;

public sealed class GBuffer : IScreenManaged
{
    private readonly Screen _screen;
    private readonly Vector2u _size;
    private Own<Texture2D>[] _colors;
    private readonly CH.Opt<CH.RenderPassColorAttachment>[] _colorsNative;
    private readonly int _colorAttachmentCount;

    public int ColorAttachmentCount => _colorAttachmentCount;
    public Screen Screen => _screen;
    public bool IsManaged => _colors.Length != 0;
    public Vector2u Size => _size;

    private GBuffer(Screen screen, Vector2u size, ReadOnlySpan<TextureFormat> formats)
    {
        var colors = new Own<Texture2D>[formats.Length];
        var colorsNative = new CH.Opt<CH.RenderPassColorAttachment>[formats.Length];
        Prepare(screen, size, formats, colors, colorsNative);
        _size = size;
        _colorAttachmentCount = formats.Length;
        _screen = screen;
        _colors = colors;
        _colorsNative = colorsNative;
    }

    private void Release()
    {
        var colors = Interlocked.Exchange(ref _colors, Array.Empty<Own<Texture2D>>());
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

    public Texture2D this[int index]
    {
        get
        {
            this.ThrowIfNotScreenManaged();
            return _colors[index].AsValue();
        }
    }

    private static void Prepare(Screen screen, Vector2u size, ReadOnlySpan<TextureFormat> formats, Span<Own<Texture2D>> colors, Span<CH.Opt<CH.RenderPassColorAttachment>> colorsNative)
    {
        for(int i = 0; i < formats.Length; i++) {
            colors[i] = Texture2D.Create(screen, new()
            {
                Size = size,
                MipLevelCount = 1,
                SampleCount = 1,
                Format = formats[i],
                Usage = TextureUsages.RenderAttachment | TextureUsages.TextureBinding | TextureUsages.CopySrc,
            });
            colorsNative[i] = new(new()
            {
                view = colors[i].AsValue().View.NativeRef,
                init = new CH.RenderPassColorBufferInit
                {
                    mode = CH.RenderPassBufferInitMode.Clear,
                    value = new Wgpu.Color(0, 0, 0, 0),
                },
            });
        }
    }
}
