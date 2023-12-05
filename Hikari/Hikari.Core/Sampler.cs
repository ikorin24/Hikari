#nullable enable
using Hikari.NativeBind;
using System;
using System.Runtime.CompilerServices;

namespace Hikari;

public sealed partial class Sampler : IScreenManaged
{
    private readonly Screen _screen;
    private Rust.OptionBox<Wgpu.Sampler> _native;
    private readonly SamplerDescriptor _desc;

    public Screen Screen => _screen;

    internal Rust.Ref<Wgpu.Sampler> NativeRef => _native.Unwrap();

    public bool IsManaged => _native.IsNone == false;

    public SamplerDescriptor Descriptor => _desc;

    [Owned(nameof(Release))]
    private Sampler(Screen screen, in SamplerDescriptor desc)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var descNative = desc.ToNative();
        _native = screen.AsRefChecked().CreateSampler(descNative);
        _screen = screen;
        _desc = desc;
    }

    ~Sampler() => Release(false);

    public void Validate() => IScreenManaged.DefaultValidate(this);

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

    private readonly u16 _anisotropyClamp;
    public u16 AnisotropyClamp
    {
        get => _anisotropyClamp;
        init
        {
            if(_anisotropyClamp == 0) {
                throw new ArgumentOutOfRangeException(nameof(AnisotropyClamp), "AnisotropyClamp must be greater than 0");
            }
            _anisotropyClamp = value;
        }
    }
    public SamplerBorderColor? BorderColor { get; init; }

    public SamplerDescriptor()
    {
        LodMinClamp = 0;
        LodMaxClamp = f32.MaxValue;
        _anisotropyClamp = 1;
        BorderColor = null;
        Compare = null;
    }

    internal CH.SamplerDescriptor ToNative()
    {
        return new CH.SamplerDescriptor
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
            anisotropy_clamp = AnisotropyClamp,
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
    [EnumMapTo(CH.SamplerBorderColor.TransparentBlack)] TransparentBlack = 0,
    [EnumMapTo(CH.SamplerBorderColor.OpaqueBlack)] OpaqueBlack = 1,
    [EnumMapTo(CH.SamplerBorderColor.OpaqueWhite)] OpaqueWhite = 2,
    [EnumMapTo(CH.SamplerBorderColor.Zero)] Zero = 3,
}
