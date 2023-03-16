#nullable enable
using Elffy.Effective.Unsafes;
using Elffy.NativeBind;
using System;
using System.Runtime.InteropServices;

namespace Elffy;

public sealed class Texture : IEngineManaged
{
    private IHostScreen? _screen;
    private Rust.OptionBox<Wgpu.Texture> _native;
    private TextureDescriptor? _desc;

    public IHostScreen? Screen => _screen;

    internal Rust.Ref<Wgpu.Texture> NativeRef => _native.Unwrap();
    internal Rust.MutRef<Wgpu.Texture> NativeMut => _native.Unwrap();

    public int Width => _desc.GetOrThrow().Size.X;
    public int Height => _desc.GetOrThrow().Size.Y;
    public int Depth => _desc.GetOrThrow().Size.Z;
    public u32 MipLevelCount => _desc.GetOrThrow().MipLevelCount;
    public u32 SampleCount => _desc.GetOrThrow().SampleCount;
    public TextureFormat Format => _desc.GetOrThrow().Format;
    public TextureUsages Usage => _desc.GetOrThrow().Usage;
    public TextureDimension Dimension => _desc.GetOrThrow().Dimension;

    public Vector2i Size
    {
        get
        {
            var size3d = _desc.GetOrThrow().Size;
            return new Vector2i(size3d.X, size3d.Y);
        }
    }

    private Texture(IHostScreen screen, Rust.Box<Wgpu.Texture> native, in TextureDescriptor desc)
    {
        _screen = screen;
        _native = native;
        _desc = desc;
    }

    ~Texture() => Release(false);

    private static readonly Action<Texture> _release = static self =>
    {
        self.Release(true);
        GC.SuppressFinalize(self);
    };

    private void Release(bool manualRelease)
    {
        if(InterlockedEx.Exchange(ref _native, Rust.OptionBox<Wgpu.Texture>.None).IsSome(out var native)) {
            native.DestroyTexture();
            if(manualRelease) {
                _screen = null;
                _desc = null;
            }
        }
    }

    public static Own<Texture> Create(IHostScreen screen, in TextureDescriptor desc)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var descNative = desc.ToNative();
        var textureNative = screen.AsRefChecked().CreateTexture(descNative);
        return Own.New(new Texture(screen, textureNative, desc), _release);
    }

    public void Write<TPixel>(u32 mipLevel, Span<TPixel> pixelData) where TPixel : unmanaged
    {
        Write(mipLevel, (ReadOnlySpan<TPixel>)pixelData);
    }

    public unsafe void Write<TPixel>(u32 mipLevel, ReadOnlySpan<TPixel> pixelData) where TPixel : unmanaged
    {
        var screenRef = this.GetScreen().AsRefChecked();
        var texture = NativeRef;
        var size = new Wgpu.Extent3d((u32)Width, (u32)Height, (u32)Depth);
        u32 bytesPerPixel = (u32)sizeof(TPixel);

        fixed(TPixel* p = pixelData) {
            screenRef.WriteTexture(
                new CE.ImageCopyTexture
                {
                    texture = texture,
                    mip_level = mipLevel,
                    aspect = CE.TextureAspect.All,
                    origin_x = 0,
                    origin_y = 0,
                    origin_z = 0,
                },
                new CE.Slice<byte>((byte*)p, pixelData.Length * sizeof(TPixel)),
                new Wgpu.ImageDataLayout
                {
                    offset = 0,
                    bytes_per_row = bytesPerPixel * size.width,
                    rows_per_image = size.height,
                },
                size);
        }
    }
}
