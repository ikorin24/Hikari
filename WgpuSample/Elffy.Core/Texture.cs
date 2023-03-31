#nullable enable
using Elffy.Imaging;
using Elffy.NativeBind;
using System;

namespace Elffy;

public sealed class Texture : IScreenManaged
{
    private readonly Screen _screen;
    private Rust.OptionBox<Wgpu.Texture> _native;
    private readonly TextureDescriptor _desc;
    private readonly Own<TextureView> _defaultView;

    public Screen Screen => _screen;
    public bool IsManaged => _native.IsNone == false;

    internal Rust.Ref<Wgpu.Texture> NativeRef => _native.Unwrap();
    internal Rust.MutRef<Wgpu.Texture> NativeMut => _native.Unwrap();

    public uint Width => _desc.Size.X;
    public uint Height => _desc.Size.Y;
    public uint Depth => _desc.Size.Z;
    public u32 MipLevelCount => _desc.MipLevelCount;
    public u32 SampleCount => _desc.SampleCount;
    public TextureFormat Format => _desc.Format;
    public TextureUsages Usage => _desc.Usage;
    public TextureDimension Dimension => _desc.Dimension;

    public TextureView View => _defaultView.AsValue();

    public Vector2u Size
    {
        get
        {
            var size3d = _desc.Size;
            return new Vector2u(size3d.X, size3d.Y);
        }
    }

    public Vector3u Extent => _desc.Size;

    private Texture(Screen screen, Rust.Box<Wgpu.Texture> native, in TextureDescriptor desc)
    {
        _screen = screen;
        _native = native;
        _desc = desc;
        _defaultView = TextureView.Create(this);
    }

    ~Texture() => Release(false);

    private void Release()
    {
        Release(true);
        GC.SuppressFinalize(this);
    }

    private void Release(bool manualRelease)
    {
        if(InterlockedEx.Exchange(ref _native, Rust.OptionBox<Wgpu.Texture>.None).IsSome(out var native)) {
            native.DestroyTexture();
            if(manualRelease) {
                _defaultView.Dispose();
            }
        }
    }

    public static Own<Texture> Create(Screen screen, in TextureDescriptor desc)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var descNative = desc.ToNative();
        var textureNative = screen.AsRefChecked().CreateTexture(descNative);
        var texture = new Texture(screen, textureNative, desc);
        return Own.RefType(texture, static x => SafeCast.As<Texture>(x).Release());
    }

    public unsafe void Write<TPixel>(u32 mipLevel, ReadOnlySpan<TPixel> pixelData) where TPixel : unmanaged
    {
        var screenRef = _screen.AsRefChecked();
        var texture = NativeRef;
        var size = new Wgpu.Extent3d((Width >> (int)mipLevel), (Height >> (int)mipLevel), Depth);
        u32 bytesPerPixel = (u32)sizeof(TPixel);

        if((ulong)size.width * (ulong)size.height != (ulong)pixelData.Length) {
            throw new ArgumentException($"length of {nameof(pixelData)} is invalid");
        }

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
