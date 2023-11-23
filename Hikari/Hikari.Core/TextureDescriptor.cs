#nullable enable
using Hikari.NativeBind;
using System;
using System.Diagnostics;

namespace Hikari;

public readonly record struct Texture1DDescriptor : ITextureDescriptor<u32>
{
    private readonly u32 _size;
    private readonly u32 _mipLevelCount;

    public required u32 Size
    {
        get => _size;
        init
        {
            if(value == 0) { ThrowSize("Size"); }
            _size = value;
        }
    }

    Vector3u ITextureDescriptor.SizeRaw => new Vector3u
    {
        X = Size,
        Y = 1,
        Z = 1,
    };

    public required u32 MipLevelCount
    {
        get => _mipLevelCount;
        init
        {
            if(value == 0) { ThrowMipLevelCount(); }
            _mipLevelCount = value;
        }
    }
    public required u32 SampleCount { get; init; }
    public TextureDimension Dimension => TextureDimension.D1;
    public required TextureFormat Format { get; init; }
    public required TextureUsages Usage { get; init; }

    public u32? MipLevelSize(u32 level)
    {
        if(level >= MipLevelCount) {
            return null;
        }
        return u32.Max(1, Size >> (int)level);
    }

    public Vector3u? MipLevelSizeRaw(u32 level)
    {
        if(level >= MipLevelCount) {
            return null;
        }
        return new Vector3u
        {
            X = u32.Max(1, Size >> (int)level),
            Y = 1,
            Z = 1,
        };
    }

    internal CH.TextureDescriptor ToNative()
    {
        if(Size == 0) { ThrowSize("Size"); }
        if(MipLevelCount == 0) { ThrowMipLevelCount(); }
        return new CH.TextureDescriptor
        {
            size = new Wgpu.Extent3d
            {
                width = Size,
                height = 1,
                depth_or_array_layers = 1,
            },
            mip_level_count = MipLevelCount,
            sample_count = SampleCount,
            dimension = Dimension.MapOrThrow(),
            format = Format.MapOrThrow(),
            usage = Usage.FlagsMap(),
        };
    }

    CH.TextureDescriptor ITextureDescriptor.ToNative() => ToNative();

    [DebuggerHidden]
    private static void ThrowMipLevelCount() => throw new ArgumentOutOfRangeException(nameof(MipLevelCount), $"The value should not be 0");

    [DebuggerHidden]
    private static void ThrowSize(string name) => throw new ArgumentOutOfRangeException(name, "The value should not be 0");
}

public readonly record struct Texture2DDescriptor : ITextureDescriptor<Vector2u>
{
    private readonly Vector2u _size;
    private readonly uint? _arrayCount;
    private readonly u32 _mipLevelCount;

    public required Vector2u Size
    {
        get => _size;
        init
        {
            if(value.X == 0) { ThrowSize("Size.X"); }
            if(value.Y == 0) { ThrowSize("Size.Y"); }
            _size = value;
        }
    }

    public uint ArrayCount
    {
        get => _arrayCount ?? 1;
        init
        {
            if(value == 0) { ThrowSize(nameof(ArrayCount)); }
            _arrayCount = value;
        }
    }

    Vector3u ITextureDescriptor.SizeRaw => new Vector3u
    {
        X = Size.X,
        Y = Size.Y,
        Z = ArrayCount,
    };

    public required u32 MipLevelCount
    {
        get => _mipLevelCount;
        init
        {
            if(value == 0) { ThrowMipLevelCount(); }
            _mipLevelCount = value;
        }
    }
    public required u32 SampleCount { get; init; }
    public TextureDimension Dimension => TextureDimension.D2;
    public required TextureFormat Format { get; init; }
    public required TextureUsages Usage { get; init; }

    public Vector2u? MipLevelSize(u32 level)
    {
        if(level >= MipLevelCount) {
            return null;
        }
        return new Vector2u
        {
            X = u32.Max(1, Size.X >> (int)level),
            Y = u32.Max(1, Size.Y >> (int)level),
        };
    }

    public Vector3u? MipLevelSizeRaw(u32 level)
    {
        if(level >= MipLevelCount) {
            return null;
        }
        return new Vector3u
        {
            X = u32.Max(1, Size.X >> (int)level),
            Y = u32.Max(1, Size.Y >> (int)level),
            Z = 1,
        };
    }

    internal CH.TextureDescriptor ToNative()
    {
        if(Size.X == 0) { ThrowSize("Size.X"); }
        if(Size.Y == 0) { ThrowSize("Size.Y"); }
        if(ArrayCount == 0) { ThrowSize(nameof(ArrayCount)); }
        if(MipLevelCount == 0) { ThrowMipLevelCount(); }
        return new CH.TextureDescriptor
        {
            size = new Wgpu.Extent3d
            {
                width = Size.X,
                height = Size.Y,
                depth_or_array_layers = ArrayCount,
            },
            mip_level_count = MipLevelCount,
            sample_count = SampleCount,
            dimension = Dimension.MapOrThrow(),
            format = Format.MapOrThrow(),
            usage = Usage.FlagsMap(),
        };
    }

    CH.TextureDescriptor ITextureDescriptor.ToNative() => ToNative();

    [DebuggerHidden]
    private static void ThrowMipLevelCount() => throw new ArgumentOutOfRangeException(nameof(MipLevelCount), $"The value should not be 0");

    [DebuggerHidden]
    private static void ThrowSize(string name) => throw new ArgumentOutOfRangeException(name, "The value should not be 0");
}

public readonly record struct Texture3DDescriptor : ITextureDescriptor<Vector3u>
{
    private readonly Vector3u _size;
    private readonly u32 _mipLevelCount;

    public required Vector3u Size
    {
        get => _size;
        init
        {
            if(value.X == 0) { ThrowSize("Size.X"); }
            if(value.Y == 0) { ThrowSize("Size.Y"); }
            if(value.Z == 0) { ThrowSize("Size.Z"); }
            _size = value;
        }
    }

    Vector3u ITextureDescriptor.SizeRaw => Size;

    public required u32 MipLevelCount
    {
        get => _mipLevelCount;
        init
        {
            if(value == 0) { ThrowMipLevelCount(); }
            _mipLevelCount = value;
        }
    }
    public required u32 SampleCount { get; init; }
    public TextureDimension Dimension => TextureDimension.D3;
    public required TextureFormat Format { get; init; }
    public required TextureUsages Usage { get; init; }

    public Vector3u? MipLevelSize(u32 level)
    {
        if(level >= MipLevelCount) {
            return null;
        }
        return new Vector3u
        {
            X = u32.Max(1, Size.X >> (int)level),
            Y = u32.Max(1, Size.Y >> (int)level),
            Z = u32.Max(1, Size.Z >> (int)level),
        };
    }

    public Vector3u? MipLevelSizeRaw(u32 level)
    {
        return MipLevelSize(level);
    }

    internal CH.TextureDescriptor ToNative()
    {
        if(Size.X == 0) { ThrowSize("Size.X"); }
        if(Size.Y == 0) { ThrowSize("Size.Y"); }
        if(Size.Z == 0) { ThrowSize("Size.Z"); }
        if(MipLevelCount == 0) { ThrowMipLevelCount(); }
        return new CH.TextureDescriptor
        {
            size = new Wgpu.Extent3d
            {
                width = Size.X,
                height = Size.Y,
                depth_or_array_layers = Size.Z,
            },
            mip_level_count = MipLevelCount,
            sample_count = SampleCount,
            dimension = Dimension.MapOrThrow(),
            format = Format.MapOrThrow(),
            usage = Usage.FlagsMap(),
        };
    }

    CH.TextureDescriptor ITextureDescriptor.ToNative() => ToNative();

    [DebuggerHidden]
    private static void ThrowMipLevelCount() => throw new ArgumentOutOfRangeException(nameof(MipLevelCount), $"The value should not be 0");

    [DebuggerHidden]
    private static void ThrowSize(string name) => throw new ArgumentOutOfRangeException(name, "The value should not be 0");
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
    [EnumMapTo(CH.TextureDimension.D1)] D1 = 0,
    [EnumMapTo(CH.TextureDimension.D2)] D2 = 1,
    [EnumMapTo(CH.TextureDimension.D3)] D3 = 2,
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
    [EnumMapTo(CH.TextureSampleType.FloatFilterable)] FloatFilterable = 0,
    [EnumMapTo(CH.TextureSampleType.FloatNotFilterable)] FloatNotFilterable = 1,
    [EnumMapTo(CH.TextureSampleType.Depth)] Depth = 2,
    [EnumMapTo(CH.TextureSampleType.Sint)] Sint = 3,
    [EnumMapTo(CH.TextureSampleType.Uint)] Uint = 4,
}

public enum TextureViewDimension
{
    [EnumMapTo(CH.TextureViewDimension.D1)] D1 = 0,
    [EnumMapTo(CH.TextureViewDimension.D2)] D2 = 1,
    [EnumMapTo(CH.TextureViewDimension.D2Array)] D2Array = 2,
    [EnumMapTo(CH.TextureViewDimension.Cube)] Cube = 3,
    [EnumMapTo(CH.TextureViewDimension.CubeArray)] CubeArray = 4,
    [EnumMapTo(CH.TextureViewDimension.D3)] D3 = 5,
}

public enum SamplerBindingType
{
    [EnumMapTo(CH.SamplerBindingType.Filtering)] Filtering = 0,
    [EnumMapTo(CH.SamplerBindingType.NonFiltering)] NonFiltering = 1,
    [EnumMapTo(CH.SamplerBindingType.Comparison)] Comparison = 2,
}

public enum BufferBindingType
{
    [EnumMapTo(CH.BufferBindingType.Uniform)] Uniform = 0,
    [EnumMapTo(CH.BufferBindingType.Storage)] Storage = 1,
    [EnumMapTo(CH.BufferBindingType.StorageReadOnly)] StorageReadOnly = 2,
}

public enum TextureAspect
{
    [EnumMapTo(CH.TextureAspect.All)] All = 0,
    [EnumMapTo(CH.TextureAspect.StencilOnly)] StencilOnly = 1,
    [EnumMapTo(CH.TextureAspect.DepthOnly)] DepthOnly = 2,
}

[Flags]
public enum TextureFormatFeatureFlags : u32
{
    [EnumMapTo(Wgpu.TextureFormatFeatureFlags.FILTERABLE)] Filterable = 1 << 0,
    [EnumMapTo(Wgpu.TextureFormatFeatureFlags.MULTISAMPLE_X2)] Multisample_x2 = 1 << 1,
    [EnumMapTo(Wgpu.TextureFormatFeatureFlags.MULTISAMPLE_X4)] Multisample_x4 = 1 << 2,
    [EnumMapTo(Wgpu.TextureFormatFeatureFlags.MULTISAMPLE_X8)] Multisample_x8 = 1 << 3,
    [EnumMapTo(Wgpu.TextureFormatFeatureFlags.MULTISAMPLE_RESOLVE)] Multisample_resolve = 1 << 4,
    [EnumMapTo(Wgpu.TextureFormatFeatureFlags.STORAGE_READ_WRITE)] Storage_read_write = 1 << 5,
    [EnumMapTo(Wgpu.TextureFormatFeatureFlags.STORAGE_ATOMICS)] Storage_atomics = 1 << 6,
    [EnumMapTo(Wgpu.TextureFormatFeatureFlags.BLENDABLE)] Blendable = 1 << 7,
}

public readonly record struct TextureFormatFeatures
{
    public readonly TextureUsages AllowedUsages { get; init; }
    public readonly TextureFormatFeatureFlags Flags { get; init; }
}
