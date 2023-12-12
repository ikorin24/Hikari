#nullable enable
using System;

namespace Hikari;

public sealed partial class GBufferProvider : IGBufferProvider
{
    private readonly Screen _screen;
    private readonly TextureFormat[] _formats;
    private Own<GBuffer> _gBuffer;
    private EventSource<IGBufferProvider> _gBufferChanged = new();
    private bool _isReleased;

    public Screen Screen => _screen;

    public Event<IGBufferProvider> GBufferChanged => _gBufferChanged.Event;

    public ReadOnlySpan<TextureFormat> Formats => _formats;

    [Owned(nameof(Release))]
    private GBufferProvider(Screen screen, Vector2u size, ReadOnlySpan<TextureFormat> formats)
    {
        _screen = screen;
        _formats = formats.ToArray();
        RecreateGBuffer(size);
    }

    private void Release()
    {
        lock(this) {
            if(_isReleased) { return; }
            _gBuffer.Dispose();
            _gBuffer = Own<GBuffer>.None;
            _isReleased = true;
        }
    }

    public static Own<GBufferProvider> CreateScreenSize(Screen screen, ReadOnlySpan<TextureFormat> formats)
    {
        var value = Create(screen, screen.ClientSize, formats);
        screen.Resized.Subscribe(x => value.AsValue().Resize(x.Size));
        return value;
    }

    public GBuffer GetCurrentGBuffer()
    {
        lock(this) {
            if(_isReleased) {
                throw new InvalidOperationException("already disposed");
            }
            return _gBuffer.AsValue();
        }
    }

    public void Resize(Vector2u newSize)
    {
        RecreateGBuffer(newSize);
    }

    private void RecreateGBuffer(Vector2u size)
    {
        GBuffer newGBuffer;
        lock(this) {
            if(_isReleased) {
                return;
            }
            if(_gBuffer.TryAsValue(out var gBuffer) && gBuffer.Size == size) {
                return;
            }
            _gBuffer.Dispose();
            _gBuffer = GBuffer.Create(_screen, size, _formats);
            newGBuffer = _gBuffer.AsValue();
        }
        _gBufferChanged.Invoke(this);
    }
}
