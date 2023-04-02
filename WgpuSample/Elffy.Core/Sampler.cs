#nullable enable
using Elffy.NativeBind;
using System;
using System.Runtime.CompilerServices;

namespace Elffy;

public sealed class Sampler : IScreenManaged
{
    private readonly Screen _screen;
    private Rust.OptionBox<Wgpu.Sampler> _native;

    public Screen Screen => _screen;

    internal Rust.Ref<Wgpu.Sampler> NativeRef => _native.Unwrap();

    public bool IsManaged => _native.IsNone == false;

    private Sampler(Screen screen, Rust.Box<Wgpu.Sampler> native)
    {
        _screen = screen;
        _native = native;
    }

    ~Sampler() => Release(false);

    private void Release()
    {
        Release(true);
        GC.SuppressFinalize(this);
    }

    private void Release(bool disposing)
    {
        if(InterlockedEx.Exchange(ref _native, Rust.OptionBox<Wgpu.Sampler>.None).IsSome(out var native)) {
            native.DestroySampler();
            if(disposing) {
            }
        }
    }

    public static Own<Sampler> Create(Screen screen, in SamplerDescriptor desc)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var descNative = desc.ToNative();
        var samplerNative = screen.AsRefChecked().CreateSampler(descNative);
        var sampler = new Sampler(screen, samplerNative);
        return Own.RefType(sampler, static x => SafeCast.As<Sampler>(x).Release());
    }

    public static Own<Sampler> NoMipmap(Screen screen, AddressMode addressMode, FilterMode magFilter, FilterMode minFilter)
    {
        return Create(screen, new SamplerDescriptor
        {
            AddressModeU = addressMode,
            AddressModeV = addressMode,
            AddressModeW = addressMode,
            MagFilter = magFilter,
            MinFilter = minFilter,
            MipmapFilter = FilterMode.Nearest,
        });
    }
}

public readonly struct SamplerDescriptor
{
    public required AddressMode AddressModeU { get; init; }
    public required AddressMode AddressModeV { get; init; }
    public required AddressMode AddressModeW { get; init; }
    public required FilterMode MagFilter { get; init; }
    public required FilterMode MinFilter { get; init; }
    public required FilterMode MipmapFilter { get; init; }

    public f32 LodMinClamp { get; init; }
    public f32 LodMaxClamp { get; init; }

    public CompareFunction? Compare { get; init; }

    private readonly u8? _anisotropyClamp;
    public u8? AnisotropyClamp
    {
        get => _anisotropyClamp;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        init
        {
            _anisotropyClamp = value switch
            {
                null or 1 or 2 or 4 or 8 or 16 => value,
                _ => throw new ArgumentException($"{value} is invalid. Valid values are null, 1, 2, 4, 8, and 16."),
            };
        }
    }
    public SamplerBorderColor? BorderColor { get; init; }

    public SamplerDescriptor()
    {
        LodMinClamp = 0;
        LodMaxClamp = f32.MaxValue;
        AnisotropyClamp = null;
        BorderColor = null;
        Compare = null;
    }

    internal CE.SamplerDescriptor ToNative()
    {
        return new CE.SamplerDescriptor
        {
            address_mode_u = AddressModeU.MapOrThrow(),
            address_mode_v = AddressModeV.MapOrThrow(),
            address_mode_w = AddressModeW.MapOrThrow(),
            mag_filter = MagFilter.MapOrThrow(),
            min_filter = MinFilter.MapOrThrow(),
            mipmap_filter = MipmapFilter.MapOrThrow(),
            lod_min_clamp = LodMinClamp,
            lod_max_clamp = LodMaxClamp,
            compare = Compare.ToNative(x => x.MapOrThrow()),
            anisotropy_clamp = (_anisotropyClamp == null) ? (u8)0 : _anisotropyClamp.Value,
            border_color = BorderColor.ToNative(x => x.MapOrThrow()),
        };
    }
}

public enum AddressMode
{
    [EnumMapTo(Wgpu.AddressMode.ClampToEdge)] ClampToEdge = 0,
    [EnumMapTo(Wgpu.AddressMode.Repeat)] Repeat = 1,
    [EnumMapTo(Wgpu.AddressMode.MirrorRepeat)] MirrorRepeat = 2,
    [EnumMapTo(Wgpu.AddressMode.ClampToBorder)] ClampToBorder = 3,
}

public enum FilterMode
{
    [EnumMapTo(Wgpu.FilterMode.Nearest)] Nearest = 0,
    [EnumMapTo(Wgpu.FilterMode.Linear)] Linear = 1,
}

public enum SamplerBorderColor
{
    [EnumMapTo(CE.SamplerBorderColor.TransparentBlack)] TransparentBlack = 0,
    [EnumMapTo(CE.SamplerBorderColor.OpaqueBlack)] OpaqueBlack = 1,
    [EnumMapTo(CE.SamplerBorderColor.OpaqueWhite)] OpaqueWhite = 2,
    [EnumMapTo(CE.SamplerBorderColor.Zero)] Zero = 3,
}
