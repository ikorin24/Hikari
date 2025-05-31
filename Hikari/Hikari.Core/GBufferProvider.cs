#nullable enable
using System;
using System.Collections.Immutable;

namespace Hikari;

public sealed partial class GBufferProvider : IGBufferProvider
{
    private static readonly ImmutableArray<TextureFormat> _formats =
    [
        TextureFormat.Rgba32Float,  // CPU: (f32, f32, f32, f32) <---> Shader: texture_2d<f32>, vec4<f32>
        TextureFormat.Rgba16Float,  // CPU: (f16, f16, f16, f16) <---> Shader: texture_2d<f32>, vec4<f32>
        TextureFormat.Rgba16Uint,   // CPU: (u16, u16, u16, u16) <---> Shader: texture_2d<u32>, vec4<u32>
    ];

    private readonly Screen _screen;
    private Own<BindGroupLayout> _bindGroupLayout;
    private Own<GBuffer> _gBuffer;
    private EventSource<IGBufferProvider> _gBufferChanged = new();
    private bool _isReleased;

    public Screen Screen => _screen;

    public Event<IGBufferProvider> GBufferChanged => _gBufferChanged.Event;
    public BindGroupLayout BindGroupLayout => _bindGroupLayout.AsValue();

    [Owned(nameof(Release))]
    private GBufferProvider(Screen screen, Vector2u size)
    {
        _screen = screen;
        _bindGroupLayout = BindGroupLayout.Create(screen, new()
        {
            Entries =
            [
                BindGroupLayoutEntry.Texture(0, ShaderStages.Fragment, new()
                {
                    Multisampled = false,
                    ViewDimension = TextureViewDimension.D2,
                    SampleType = TextureSampleType.FloatNotFilterable,
                }),
                BindGroupLayoutEntry.Texture(1, ShaderStages.Fragment, new()
                {
                    Multisampled = false,
                    ViewDimension = TextureViewDimension.D2,
                    SampleType = TextureSampleType.FloatNotFilterable,
                }),
                BindGroupLayoutEntry.Texture(2, ShaderStages.Fragment, new()
                {
                    Multisampled = false,
                    ViewDimension = TextureViewDimension.D2,
                    SampleType = TextureSampleType.Uint,
                }),
            ]
        });
        RecreateGBuffer(size);
    }

    private void Release()
    {
        lock(this) {
            if(_isReleased) { return; }
            _gBuffer.Dispose();
            _gBuffer = Own<GBuffer>.None;
            _bindGroupLayout.Dispose();
            _bindGroupLayout = Own.None;
            _isReleased = true;
        }
    }

    public static Own<GBufferProvider> CreateScreenSize(Screen screen)
    {
        var value = Create(screen, screen.ClientSize);
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
            _gBuffer = GBuffer.Create(_screen, size, _bindGroupLayout.AsValue(), _formats.AsSpan());
            newGBuffer = _gBuffer.AsValue();
        }
        _gBufferChanged.Invoke(this);
    }
}
