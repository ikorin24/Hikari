#nullable enable
using System;
using System.Collections.Immutable;
using System.Threading;

namespace Hikari;

public sealed partial class GBuffer
{
    private readonly Screen _screen;
    private readonly Vector2u _size;
    private Texture2D[]? _colors;
    private EventSource<GBuffer> _disposed;
    private Own<BindGroup> _bindGroup;

    public Event<GBuffer> Disposed => _disposed.Event;
    public Screen Screen => _screen;
    public Vector2u Size => _size;
    public ReadOnlySpan<Texture2D> Textures => _colors;
    public BindGroup BindGroup => _bindGroup.AsValue();

    [Owned(nameof(Release))]
    private GBuffer(Screen screen, Vector2u size, BindGroupLayout bindGroupLayout, ReadOnlySpan<TextureFormat> formats)
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
        var entries = new BindGroupEntry[colors.Length];
        for(uint i = 0; i < entries.Length; i++) {
            entries[i] = BindGroupEntry.TextureView(i, colors[i].View);
        }
        _bindGroup = BindGroup.Create(screen, new()
        {
            Layout = bindGroupLayout,
            Entries = entries.AsImmutableArray(),
        });
    }

    private void Release()
    {
        if(Interlocked.Exchange(ref _colors, null) is null) {
            return;
        }
        _disposed.Invoke(this);
        _bindGroup.Dispose();
        _bindGroup = Own.None;
    }
}
