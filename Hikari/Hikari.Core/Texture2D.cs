#nullable enable
using Cysharp.Threading.Tasks;
using Hikari.Imaging;
using Hikari.NativeBind;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hikari;

public sealed partial class Texture2D : ITexture2DProvider, IScreenManaged
{
    private readonly Screen _screen;
    private Rust.OptionBox<Wgpu.Texture> _native;
    private readonly Texture2DDescriptor _desc;
    private readonly Own<TextureView> _defaultView;

    public Screen Screen => _screen;
    public bool IsManaged => _native.IsNone == false;

    internal Rust.Ref<Wgpu.Texture> NativeRef => _native.Unwrap();

    public uint Width => _desc.Size.X;
    public uint Height => _desc.Size.Y;
    public u32 MipLevelCount => _desc.MipLevelCount;
    public u32 SampleCount => _desc.SampleCount;
    public TextureFormat Format => _desc.Format;
    public TextureUsages Usage => _desc.Usage;
    public TextureDimension Dimension => _desc.Dimension;

    public bool CanWrite => Usage.HasFlag(TextureUsages.CopyDst);
    public bool CanRead => Usage.HasFlag(TextureUsages.CopySrc);

    public TextureView View => _defaultView.AsValue();

    public Vector2u Size => _desc.Size;

    Event<ITexture2DProvider> ITextureProvider.TextureChanged => Event<ITexture2DProvider>.Never;

    Event<ITextureViewProvider> ITextureViewProvider.TextureViewChanged => Event<ITextureViewProvider>.Never;

    private Texture2D(Screen screen, Rust.Box<Wgpu.Texture> native, in Texture2DDescriptor desc)
    {
        _screen = screen;
        _native = native;
        _desc = desc;
        _defaultView = TextureView.Create(this);
    }

    [Owned(nameof(Release))]
    private Texture2D(Screen screen, in Texture2DDescriptor desc)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var descNative = desc.ToNative();
        _native = screen.AsRefChecked().CreateTexture(in descNative);
        _screen = screen;
        _desc = desc;
        _defaultView = TextureView.Create(this);
    }

    ~Texture2D() => Release(false);

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

    public Texture2DDescriptor GetDescriptor() => _desc;

    public unsafe static Own<Texture2D> CreateFromRawData(Screen screen, in Texture2DDescriptor desc, ReadOnlySpan<u8> rawData)
    {
        return TextureHelper.CreateFromRawData(screen, desc, rawData, &Callback);

        static Own<Texture2D> Callback(
            Screen screen,
            Rust.Box<Wgpu.Texture> textureNative,
            in Texture2DDescriptor desc)
        {
            var texture = new Texture2D(screen, textureNative, desc);
            return Own.New(texture, static x => SafeCast.As<Texture2D>(x).Release());
        }
    }

    public unsafe static Own<Texture2D> CreateWithAutoMipmap(Screen screen, ImageView image, TextureFormat format, TextureUsages usage, uint? mipLevelCount = null)
    {
        var desc = new Texture2DDescriptor
        {
            Size = new Vector2u((uint)image.Width, (uint)image.Height),
            MipLevelCount = mipLevelCount switch
            {
                not null => mipLevelCount.Value,
                null => uint.Log2(uint.Min((uint)image.Width, (uint)image.Height)),
            },
            SampleCount = 1,
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

        Texture2D texture;
        switch(desc.MipLevelCount) {
            case 0: {
                throw new ArgumentException($"{nameof(Texture2DDescriptor.MipLevelCount)} should be 1 or larger");
            }
            case 1: {
                var descNative = desc.ToNative();
                Rust.Box<Wgpu.Texture> textureNative;
                var pixelBytes = image.GetPixels().MarshalCast<ColorByte, u8>();
                fixed(u8* p = pixelBytes) {
                    var data = new CH.Slice<u8>(p, (usize)pixelBytes.Length);
                    textureNative = screen.AsRefChecked().CreateTextureWithData(descNative, data);
                }
                texture = new Texture2D(screen, textureNative, desc);
                break;
            }
            default: {
                Span<(Vector3u MipSize, u32 ByteLength)> mipData = stackalloc (Vector3u, u32)[(int)desc.MipLevelCount];
                TextureHelper.CalcMipDataSize(desc, mipData, out var totalByteSize);
                u8* p = (u8*)NativeMemory.Alloc(totalByteSize);
                try {
                    var ((width0, height0, _), mip0Bytelen) = mipData[0];
                    Debug.Assert(width0 == image.Size.X);
                    Debug.Assert(height0 == image.Size.Y);
                    var mipmap0 = new ImageViewMut((ColorByte*)p, (int)width0, (int)height0);
                    image.GetPixels().CopyTo(mipmap0.GetPixels());

                    usize byteOffset = mip0Bytelen;
                    var mipmapBefore = mipmap0.AsReadOnly();
                    for(int level = 1; level < desc.MipLevelCount; level++) {
                        var ((w, h, _), mipBytelen) = mipData[level];
                        var mipmap = new ImageViewMut((ColorByte*)(p + byteOffset), (int)w, (int)h);
                        mipmapBefore.ResizeTo(mipmap);
                        byteOffset += mipBytelen;
                        mipmapBefore = mipmap;
                    }
                    var data = new CH.Slice<u8>(p, totalByteSize);
                    var descNative = desc.ToNative();
                    var textureNative = screen.AsRefChecked().CreateTextureWithData(descNative, data);
                    texture = new Texture2D(screen, textureNative, desc);
                }
                finally {
                    NativeMemory.Free(p);
                }
                break;
            }
        }
        return Own.New(texture, static x => SafeCast.As<Texture2D>(x).Release());
    }

    public static Own<Texture2D> Create1x1Rgba8Unorm(Screen screen, TextureUsages usage, ColorByte value)
    {
        var desc = new Texture2DDescriptor
        {
            Format = TextureFormat.Rgba8Unorm,
            MipLevelCount = 1,
            Size = new Vector2u(1, 1),
            SampleCount = 1,
            Usage = usage,
        };

        if(value == default) {
            return Create(screen, desc);
        }
        else {
            var data = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ColorByte, byte>(ref value), Unsafe.SizeOf<ColorByte>());
            return CreateFromRawData(screen, desc, data);
        }
    }

    public static Own<Texture2D> Create1x1Rgba8UnormSrgb(Screen screen, TextureUsages usage, ColorByte value)
    {
        var desc = new Texture2DDescriptor
        {
            Format = TextureFormat.Rgba8UnormSrgb,
            MipLevelCount = 1,
            Size = new Vector2u(1, 1),
            SampleCount = 1,
            Usage = usage,
        };
        if(value == default) {
            return Create(screen, desc);
        }
        else {
            var data = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ColorByte, byte>(ref value), Unsafe.SizeOf<ColorByte>());
            return CreateFromRawData(screen, desc, data);
        }
    }

    public unsafe void Write<TPixel>(u32 mipLevel, ReadOnlySpan<TPixel> pixelData) where TPixel : unmanaged
    {
        if(CanWrite == false) {
            ThrowHelper.ThrowInvalidOperation("Texture is not writable");
        }
        var screenRef = _screen.AsRefChecked();
        var texture = NativeRef;
        var size = new Wgpu.Extent3d((Width >> (int)mipLevel), (Height >> (int)mipLevel), 1);
        u32 bytesPerPixel = (u32)sizeof(TPixel);

        if((ulong)size.width * (ulong)size.height != (ulong)pixelData.Length) {
            throw new ArgumentException($"length of {nameof(pixelData)} is invalid");
        }

        fixed(TPixel* p = pixelData) {
            screenRef.WriteTexture(
                new CH.ImageCopyTexture
                {
                    texture = texture,
                    mip_level = mipLevel,
                    aspect = CH.TextureAspect.All,
                    origin_x = 0,
                    origin_y = 0,
                    origin_z = 0,
                },
                new CH.Slice<byte>((byte*)p, pixelData.Length * sizeof(TPixel)),
                new Wgpu.ImageDataLayout
                {
                    offset = 0,
                    bytes_per_row = bytesPerPixel * size.width,
                    rows_per_image = size.height,
                },
                size);
        }
    }

    public Vector2u MipLevelSize(u32 mip)
    {
        return _desc.MipLevelSize(mip).GetOrThrow();
    }

    public void ReadCallback(
        ReadOnlySpanAction<byte, Texture2D> onRead,
        Action<Exception>? onException = null)
    {
        CheckUsageFlag(TextureUsages.CopySrc, Usage);

        var mip = 0u;

        var mipSize = _desc.MipLevelSizeRaw(mip).GetOrThrow();
        var mipInfo = Format.MipInfo(mipSize);
        var screen = Screen;
        var source = new CH.ImageCopyTexture
        {
            aspect = CH.TextureAspect.All,
            mip_level = mip,
            origin_x = 0,
            origin_y = 0,
            origin_z = 0,
            texture = NativeRef,
        };
        var size = new Wgpu.Extent3d
        {
            width = Width,
            height = Height,
            depth_or_array_layers = 1,
        };
        var layout = new Wgpu.ImageDataLayout
        {
            offset = 0,
            bytes_per_row = mipInfo.BytesPerRow,
            rows_per_image = mipInfo.RowCount,
        };
        uint bufferSize = layout.bytes_per_row * layout.rows_per_image;

        // make bufferSize multiply of COPY_BYTES_PER_ROW_ALIGNMENT
        bufferSize = (bufferSize + EngineConsts.COPY_BYTES_PER_ROW_ALIGNMENT - 1) & ~(EngineConsts.COPY_BYTES_PER_ROW_ALIGNMENT - 1);

        using(var buffer = Buffer.Create(screen, (nuint)bufferSize, BufferUsages.CopySrc | BufferUsages.CopyDst)) {
            var bufValue = buffer.AsValue();
            EngineCore.CopyTextureToBuffer(screen.AsRefChecked(), source, size, bufValue.NativeRef, layout);
            bufValue.ReadCallback((bytes, _) => onRead(bytes, this), onException);
        }
    }

    public UniTask<int> Read(Memory<byte> dest)
    {
        var completionSource = new UniTaskCompletionSource<int>();
        ReadCallback((pixels, texture) =>
        {
            pixels.CopyTo(dest.Span);
            completionSource.TrySetResult(pixels.Length);
        }, (ex) =>
        {
            completionSource.TrySetException(ex);
        });
        return completionSource.Task;
    }

    public UniTask<byte[]> ReadToArray()
    {
        var completionSource = new UniTaskCompletionSource<byte[]>();
        ReadCallback((pixels, texture) =>
        {
            completionSource.TrySetResult(pixels.ToArray());
        }, (ex) =>
        {
            completionSource.TrySetException(ex);
        });
        return completionSource.Task;
    }

    [DebuggerHidden]
    private static void CheckUsageFlag(TextureUsages needed, TextureUsages actual)
    {
        if(actual.HasFlag(needed) == false) {
            throw new InvalidOperationException($"'{needed}' flag is needed, but the flag the texture has is '{actual}'.");
        }
    }

    Vector2u ITexture2DProvider.GetCurrentSize() => Size;

    uint ITexture2DProvider.GetCurrentMipLevelCount() => MipLevelCount;

    uint ITexture2DProvider.GetCurrentSampleCount() => SampleCount;

    TextureFormat ITexture2DProvider.GetCurrentFormat() => Format;

    TextureUsages ITexture2DProvider.GetCurrentUsage() => Usage;

    TextureDimension ITexture2DProvider.GetCurrentDimension() => Dimension;

    Rust.Ref<Wgpu.Texture> ITextureProvider.GetCurrentTexture() => NativeRef;

    Rust.Ref<Wgpu.TextureView> ITextureViewProvider.GetCurrentTextureView() => View.NativeRef;
}
