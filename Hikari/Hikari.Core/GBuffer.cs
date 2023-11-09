#nullable enable
using System;
using System.Threading;

namespace Hikari;

public sealed class GBuffer : IScreenManaged
{
    private readonly Screen _screen;
    private readonly Vector2u _size;
    private Texture2D[]? _colors;
    private EventSource<GBuffer> _disposed;

    public Event<GBuffer> Disposed => _disposed.Event;
    public Screen Screen => _screen;
    public bool IsManaged => _colors != null;
    public Vector2u Size => _size;
    public ReadOnlySpan<Texture2D> Textures => _colors;

    private GBuffer(Screen screen, Vector2u size, ReadOnlySpan<TextureFormat> formats)
    {
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
}
