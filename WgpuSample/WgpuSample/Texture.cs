#nullable enable
using Elffy.Bind;
using System;
using System.Runtime.CompilerServices;

namespace Elffy;

public sealed class Texture : IEngineManaged, IDisposable
{
    private IHostScreen? _screen;
    private Box<Wgpu.Texture> _native;
    private Vector3i _size;

    public IHostScreen? Screen => _screen;

    internal Ref<Wgpu.Texture> NativeRef => _native.AsRefChecked();
    internal MutRef<Wgpu.Texture> NativeMut => _native.AsMutChecked();

    public bool IsEmpty => _native.IsInvalid;

    public int Width => _size.X;
    public int Height => _size.Y;
    public int Depth => _size.Z;

    public Vector2i Size => new Vector2i(_size.X, _size.Y);

    private Texture(IHostScreen screen, Box<Wgpu.Texture> native, Vector3i size)
    {
        _screen = screen;
        _native = native;
        _size = size;
    }

    public static Texture Create(IHostScreen screen, in TextureDescriptor desc)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var descNative = desc.ToNative();
        var texture = screen.AsRefChecked().CreateTexture(descNative);
        var size = desc.Size;
        return new Texture(screen, texture, size);
    }

    public static Texture CreateLoad(IHostScreen screen, ReadOnlySpan<byte> pixelData, in TextureDescriptor desc)
    {
        var texture = Create(screen, desc);
        try {
            texture.Load(pixelData);
        }
        catch {
            texture.Dispose();
            throw;
        }
        return texture;
    }

    public unsafe void Load(ReadOnlySpan<byte> pixelData)
    {
        var screenRef = this.GetScreen().AsRefChecked();
        var texture = _native.AsRefChecked();
        var size = new Wgpu.Extent3d((u32)_size.X, (u32)_size.Y, (u32)_size.Z);

        fixed(byte* p = pixelData) {
            screenRef.WriteTexture(
                new CE.ImageCopyTexture
                {
                    texture = texture,
                    mip_level = 0,
                    aspect = CE.TextureAspect.All,
                    origin_x = 0,
                    origin_y = 0,
                    origin_z = 0,
                },
                new Slice<byte>(p, pixelData.Length),
                new Wgpu.ImageDataLayout
                {
                    offset = 0,
                    bytes_per_row = 4 * size.width,
                    rows_per_image = size.height,
                },
                size);
        }
    }

    public void Dispose()
    {
        if(_native.IsInvalid) {
            return;
        }
        _native.DestroyTexture();
        _native = Box<Wgpu.Texture>.Invalid;
        _screen = null;
    }
}

public readonly struct TextureDescriptor
{
    public required Vector3i Size { get; init; }
    public required u32 MipLevelCount { get; init; }
    public required u32 SampleCount { get; init; }
    public required TextureDimension Dimension { get; init; }
    public required TextureFormat Format { get; init; }
    public required TextureUsages Usage { get; init; }

    internal CE.TextureDescriptor ToNative()
    {
        return new CE.TextureDescriptor
        {
            size = new Wgpu.Extent3d
            {
                width = checked((u32)Size.X),
                height = checked((u32)Size.Y),
                depth_or_array_layers = checked((u32)Size.Z),
            },
            mip_level_count = MipLevelCount,
            sample_count = SampleCount,
            dimension = Dimension.MapOrThrow(),
            format = Format.MapOrThrow(),
            usage = Usage.FlagsMap(),
        };
    }
}

[Flags]
public enum TextureUsages : u32
{
    [EnumMapTo(Wgpu.TextureUsages.COPY_SRC)] CopySrc = 1 << 0,
    [EnumMapTo(Wgpu.TextureUsages.COPY_DST)] CopyDst = 1 << 1,
    [EnumMapTo(Wgpu.TextureUsages.TEXTURE_BINDING)] TextureBinding = 1 << 2,
    [EnumMapTo(Wgpu.TextureUsages.STORAGE_BINDING)] StorageBinding = 1 << 3,
    [EnumMapTo(Wgpu.TextureUsages.RENDER_ATTACHMENT)] RenderAttachment = 1 << 4,
}

public enum TextureDimension
{
    [EnumMapTo(CE.TextureDimension.D1)] D1 = 0,
    [EnumMapTo(CE.TextureDimension.D2)] D2 = 1,
    [EnumMapTo(CE.TextureDimension.D3)] D3 = 2,
}

[Flags]
public enum BufferUsages : u32
{
    [EnumMapTo(Wgpu.BufferUsages.MAP_READ)] MapRead = 1 << 0,
    [EnumMapTo(Wgpu.BufferUsages.MAP_WRITE)] MapWrite = 1 << 1,
    [EnumMapTo(Wgpu.BufferUsages.COPY_SRC)] CopySrc = 1 << 2,
    [EnumMapTo(Wgpu.BufferUsages.COPY_DST)] CopyDst = 1 << 3,
    [EnumMapTo(Wgpu.BufferUsages.INDEX)] Index = 1 << 4,
    [EnumMapTo(Wgpu.BufferUsages.VERTEX)] Vertex = 1 << 5,
    [EnumMapTo(Wgpu.BufferUsages.UNIFORM)] Uniform = 1 << 6,
    [EnumMapTo(Wgpu.BufferUsages.STORAGE)] Storage = 1 << 7,
    [EnumMapTo(Wgpu.BufferUsages.INDIRECT)] Indirect = 1 << 8,
}

[Flags]
public enum ShaderStages : u32
{
    [EnumMapTo(Wgpu.ShaderStages.NONE)] None = 0,
    [EnumMapTo(Wgpu.ShaderStages.VERTEX)] Vertex = 1 << 0,
    [EnumMapTo(Wgpu.ShaderStages.FRAGMENT)] Fragment = 1 << 1,
    [EnumMapTo(Wgpu.ShaderStages.COMPUTE)] Compute = 1 << 2,
}

public enum TextureSampleType
{
    [EnumMapTo(CE.TextureSampleType.FloatFilterable)] FloatFilterable = 0,
    [EnumMapTo(CE.TextureSampleType.FloatNotFilterable)] FloatNotFilterable = 1,
    [EnumMapTo(CE.TextureSampleType.Depth)] Depth = 2,
    [EnumMapTo(CE.TextureSampleType.Sint)] Sint = 3,
    [EnumMapTo(CE.TextureSampleType.Uint)] Uint = 4,
}

public enum TextureViewDimension
{
    [EnumMapTo(CE.TextureViewDimension.D1)] D1 = 0,
    [EnumMapTo(CE.TextureViewDimension.D2)] D2 = 1,
    [EnumMapTo(CE.TextureViewDimension.D2Array)] D2Array = 2,
    [EnumMapTo(CE.TextureViewDimension.Cube)] Cube = 3,
    [EnumMapTo(CE.TextureViewDimension.CubeArray)] CubeArray = 4,
    [EnumMapTo(CE.TextureViewDimension.D3)] D3 = 5,
}

public enum SamplerBindingType
{
    [EnumMapTo(CE.SamplerBindingType.Filtering)] Filtering = 0,
    [EnumMapTo(CE.SamplerBindingType.NonFiltering)] NonFiltering = 1,
    [EnumMapTo(CE.SamplerBindingType.Comparison)] Comparison = 2,
}

public enum BufferBindingType
{
    [EnumMapTo(CE.BufferBindingType.Uniform)] Uniform = 0,
    [EnumMapTo(CE.BufferBindingType.Storate)] Storate = 1,
    [EnumMapTo(CE.BufferBindingType.StorateReadOnly)] StorateReadOnly = 2,
}
