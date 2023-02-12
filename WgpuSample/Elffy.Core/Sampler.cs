#nullable enable
using Elffy.NativeBind;
using System;

namespace Elffy;

public sealed class Sampler : IEngineManaged
{
    private IHostScreen? _screen;
    private Box<Wgpu.Sampler> _native;

    public IHostScreen? Screen => _screen;

    internal Ref<Wgpu.Sampler> NativeRef => _native;

    private Sampler(IHostScreen screen, Box<Wgpu.Sampler> native)
    {
        _screen = screen;
        _native = native;
    }

    ~Sampler() => Release(false);

    private static readonly Action<Sampler> _release = static self =>
    {
        self.Release(true);
        GC.SuppressFinalize(self);
    };

    private void Release(bool disposing)
    {
        var native = Box.SwapClear(ref _native);
        if(native.IsInvalid) {
            return;
        }
        native.DestroySampler();
        if(disposing) {
            _screen = null;
        }
    }

    public static Own<Sampler> Create(IHostScreen screen, in SamplerDescriptor desc)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var descNative = desc.ToNative();
        var sampler = screen.AsRefChecked().CreateSampler(descNative);
        return Own.New(new Sampler(screen, sampler), _release);
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
    public required f32 LodMinClamp { get; init; }
    public required f32 LodMaxClamp { get; init; }
    public required CompareFunction? Compare { get; init; }
    public required u8 AnisotropyClamp { get; init; }
    public required SamplerBorderColor? BorderColor { get; init; }

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
    [EnumMapTo(CE.SamplerBorderColor.TransparentBlack)] TransparentBlack = 0,
    [EnumMapTo(CE.SamplerBorderColor.OpaqueBlack)] OpaqueBlack = 1,
    [EnumMapTo(CE.SamplerBorderColor.OpaqueWhite)] OpaqueWhite = 2,
    [EnumMapTo(CE.SamplerBorderColor.Zero)] Zero = 3,
}
