#nullable enable
using System;
using System.Threading;

namespace Hikari;

public sealed partial class GBuffer
{
    private readonly Screen _screen;
    private readonly Vector2u _size;
    private Texture2D[]? _colors;
    private EventSource<GBuffer> _disposed;

    public Event<GBuffer> Disposed => _disposed.Event;
    public Screen Screen => _screen;
    public Vector2u Size => _size;
    public ReadOnlySpan<Texture2D> Textures => _colors;

    [Owned(nameof(Release))]
    private GBuffer(Screen screen, Vector2u size, ReadOnlySpan<TextureFormat> formats)
    {
        if(formats.IsEmpty) {
            throw new ArgumentException("formats is empty", nameof(formats));
        }
        var colors = new Texture2D[formats.Length];
        for(int i = 0; i < colors.Length; i++) {
            colors[i] = Texture2D.Create(screen, new()
            {
                Size = size,
                MipLevelCount = 1,
                SampleCount = 1,
                Format = formats[i],
                Usage = TextureUsages.RenderAttachment | TextureUsages.TextureBinding | TextureUsages.CopySrc,
            }).DisposeOn(Disposed);
        }
        _size = size;
        _screen = screen;
        _colors = colors;
    }

    private void Release()
    {
        if(Interlocked.Exchange(ref _colors, null) is null) {
            return;
        }
        _disposed.Invoke(this);
    }
}
