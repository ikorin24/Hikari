#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Effective;
using Elffy.Imaging;
using Elffy.NativeBind;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

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

    public void Validate() => IScreenManaged.DefaultValidate(this);

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
        return Own.New(texture, static x => SafeCast.As<Texture>(x).Release());
    }

    public unsafe static Own<Texture> CreateFromRawData(Screen screen, in TextureDescriptor desc, ReadOnlySpan<u8> data)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var descNative = desc.ToNative();
        Rust.Box<Wgpu.Texture> textureNative;
        fixed(u8* p = data) {
            textureNative = screen
                .AsRefChecked()
                .CreateTextureWithData(
                    descNative,
                    new CE.Slice<byte>(p, data.Length)
                );
        }
        var texture = new Texture(screen, textureNative, desc);
        return Own.New(texture, static x => SafeCast.As<Texture>(x).Release());
    }

    public unsafe static Own<Texture> CreateWithAutoMipmap(Screen screen, ReadOnlyImageRef image, TextureFormat format, TextureUsages usage, uint? mipLevelCount = null)
    {
        var desc = new TextureDescriptor
        {
            Size = new Vector3u((uint)image.Width, (uint)image.Height, 1),
            MipLevelCount = mipLevelCount.GetValueOrDefault(
                uint.Log2(uint.Min((uint)image.Width, (uint)image.Height))
            ),
            SampleCount = 1,
            Dimension = TextureDimension.D2,
            Format = format,
            Usage = usage,
        };

        ArgumentNullException.ThrowIfNull(screen);
        var isRgba8Format = desc.Format is
            TextureFormat.Rgba8Sint or
            TextureFormat.Rgba8Snorm or
            TextureFormat.Rgba8Uint or
            TextureFormat.Rgba8Unorm or
            TextureFormat.Rgba8UnormSrgb;
        if(isRgba8Format == false) {
            throw new ArgumentException("not supported format");
        }
        if(desc.Dimension != TextureDimension.D2) {
            throw new ArgumentException("2D texture is only supported.");
        }

        Texture texture;
        switch(desc.MipLevelCount) {
            case 0: {
                throw new ArgumentException($"{nameof(TextureDescriptor.MipLevelCount)} should be 1 or larger");
            }
            case 1: {
                var descNative = desc.ToNative();
                Rust.Box<Wgpu.Texture> textureNative;
                var pixelBytes = image.GetPixels().MarshalCast<ColorByte, u8>();
                fixed(u8* p = pixelBytes) {
                    var data = new CE.Slice<u8>(p, (usize)pixelBytes.Length);
                    textureNative = screen.AsRefChecked().CreateTextureWithData(descNative, data);
                }
                texture = new Texture(screen, textureNative, desc);
                break;
            }
            default: {
                Span<(Vector3u MipSize, u32 ByteLength)> mipData = stackalloc (Vector3u, u32)[(int)desc.MipLevelCount];
                CalcMipDataSize(desc, mipData, out var totalByteSize);
                u8* p = (u8*)NativeMemory.Alloc(totalByteSize);
                try {
                    var ((width0, height0, _), mip0Bytelen) = mipData[0];
                    Debug.Assert(width0 == image.Size.X);
                    Debug.Assert(height0 == image.Size.Y);
                    var mipmap0 = new ImageRef((ColorByte*)p, (int)width0, (int)height0);
                    image.GetPixels().CopyTo(mipmap0.GetPixels());

                    usize byteOffset = mip0Bytelen;
                    var mipmapBefore = mipmap0.AsReadOnly();
                    for(int level = 1; level < desc.MipLevelCount; level++) {
                        var ((w, h, _), mipBytelen) = mipData[level];
                        var mipmap = new ImageRef((ColorByte*)(p + byteOffset), (int)w, (int)h);
                        mipmapBefore.ResizeTo(mipmap);
                        byteOffset += mipBytelen;
                        mipmapBefore = mipmap;
                    }
                    var data = new CE.Slice<u8>(p, totalByteSize);
                    var descNative = desc.ToNative();
                    var textureNative = screen.AsRefChecked().CreateTextureWithData(descNative, data);
                    texture = new Texture(screen, textureNative, desc);
                }
                finally {
                    NativeMemory.Free(p);
                }
                break;
            }
        }
        return Own.New(texture, static x => SafeCast.As<Texture>(x).Release());
    }

    private static void CalcMipDataSize(in TextureDescriptor desc, Span<(Vector3u MipSize, u32 ByteLength)> mipData, out usize totalByteSize)
    {
        totalByteSize = 0;
        var formatInfo = desc.Format.MapOrThrow().TextureFormatInfo();
        var arrayLayerCount = desc.ArrayLayerCount();
        for(uint layer = 0; layer < arrayLayerCount; layer++) {
            for(uint mip = 0; mip < desc.MipLevelCount; mip++) {
                var mipSize = desc.MipLevelSize(mip).GetOrThrow();
                if(desc.Dimension != TextureDimension.D3) {
                    mipSize.Z = 1;
                }
                var mipPhysicalSize = physicalSize(mipSize, formatInfo);
                u32 widthBlocks = mipPhysicalSize.X / formatInfo.block_dimensions.Value1;
                u32 heightBlocks = mipPhysicalSize.Y / formatInfo.block_dimensions.Value2;
                u32 bytesPerRow = widthBlocks * formatInfo.block_size;
                u32 dataSize = bytesPerRow * heightBlocks * mipSize.Z;
                mipData[(int)mip] = (MipSize: mipSize, ByteLength: dataSize);
                totalByteSize += dataSize;
            }
        }

        static Vector3u physicalSize(Vector3u mipSize, in CE.TextureFormatInfo formatInfo)
        {
            var (w, h) = formatInfo.block_dimensions;
            var block_width = (u32)w;
            var block_height = (u32)h;

            var width = ((mipSize.X + block_width - 1) / block_width) * block_width;
            var height = ((mipSize.Y + block_height - 1) / block_height) * block_height;

            return new Vector3u
            {
                X = width,
                Y = height,
                Z = mipSize.Z,
            };
        }
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

    public void ReadCallback<TPixel>(
        ReadOnlySpanAction<TPixel> onRead,
        Action<Exception>? onException = null)
    where TPixel : unmanaged
    {
        var screen = Screen;
        if(Usage.HasFlag(TextureUsages.CopySrc) == false) {
            throw new InvalidOperationException();
        }
        u32 bytesPerPixel;
        unsafe {
            bytesPerPixel = (u32)sizeof(TPixel);
        }

        var source = new CE.ImageCopyTexture
        {
            aspect = CE.TextureAspect.All,
            mip_level = 0,
            origin_x = 0,
            origin_y = 0,
            origin_z = 0,
            texture = NativeRef,
        };
        var size = new Wgpu.Extent3d
        {
            width = Width,
            height = Height,
            depth_or_array_layers = Depth,
        };
        var layout = new Wgpu.ImageDataLayout
        {
            offset = 0,
            bytes_per_row = bytesPerPixel * size.width,
            rows_per_image = size.height,
        };
        using(var buffer = Buffer.Create(screen, layout.bytes_per_row * layout.rows_per_image, BufferUsages.CopyDst | BufferUsages.CopySrc)) {
            var bufValue = buffer.AsValue();
            EngineCore.CopyTextureToBuffer(screen.AsRefChecked(), source, size, bufValue.NativeRef, layout);
            bufValue.ReadCallback((bytes) =>
            {
                var pixels = MemoryMarshal.Cast<byte, TPixel>(bytes);
                onRead(pixels);
            }, onException);
        }
    }

    public UniTask<int> Read<TPixel>(Memory<TPixel> dest) where TPixel : unmanaged
    {
        var completionSource = new UniTaskCompletionSource<int>();
        ReadCallback<TPixel>((pixels) =>
        {
            pixels.CopyTo(dest.Span);
            completionSource.TrySetResult(pixels.Length);
        }, (ex) =>
        {
            completionSource.TrySetException(ex);
        });
        return completionSource.Task;
    }

    public UniTask<TPixel[]> ReadToArray<TPixel>() where TPixel : unmanaged
    {
        var completionSource = new UniTaskCompletionSource<TPixel[]>();
        ReadCallback<TPixel>((pixels) =>
        {
            completionSource.TrySetResult(pixels.ToArray());
        }, (ex) =>
        {
            completionSource.TrySetException(ex);
        });
        return completionSource.Task;
    }
}
