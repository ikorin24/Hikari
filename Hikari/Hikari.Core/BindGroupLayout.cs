#nullable enable
using Hikari.NativeBind;
using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hikari;

public sealed class BindGroupLayout : IScreenManaged
{
    private readonly Screen _screen;
    private Rust.OptionBox<Wgpu.BindGroupLayout> _native;

    public Screen Screen => _screen;

    internal Rust.Ref<Wgpu.BindGroupLayout> NativeRef => _native.Unwrap();

    public bool IsManaged => _native.IsNone == false;

    private BindGroupLayout(Screen screen, Rust.Box<Wgpu.BindGroupLayout> native)
    {
        _screen = screen;
        _native = native;
    }

    ~BindGroupLayout() => Release(false);

    private static readonly Action<BindGroupLayout> _release = static self =>
    {
        self.Release(true);
        GC.SuppressFinalize(self);
    };

    private void Release(bool disposing)
    {
        if(InterlockedEx.Exchange(ref _native, Rust.OptionBox<Wgpu.BindGroupLayout>.None).IsSome(out var native)) {
            native.DestroyBindGroupLayout();
            if(disposing) {
            }
        }
    }

    public void Validate() => IScreenManaged.DefaultValidate(this);

    public unsafe static Own<BindGroupLayout> Create(Screen screen, in BindGroupLayoutDescriptor desc)
    {
        using var pins = new PinHandleHolder();
        var descNative = desc.ToNative(pins);
        var bindGroupLayoutNative = screen.AsRefChecked().CreateBindGroupLayout(descNative);
        var bindGroupLayout = new BindGroupLayout(screen, bindGroupLayoutNative);
        return Own.New(bindGroupLayout, static x => _release(SafeCast.As<BindGroupLayout>(x)));
    }
}

public readonly struct BindGroupLayoutDescriptor
{
    public required ImmutableArray<BindGroupLayoutEntry> Entries { get; init; }

    internal CH.BindGroupLayoutDescriptor ToNative(PinHandleHolder pins)
    {
        return new CH.BindGroupLayoutDescriptor
        {
            entries = Entries.SelectToArray(pins, static (entry, pins) => entry.ToNative(pins)).AsFixedSlice(pins),
        };
    }
}

internal interface IBindingTypeData
{
    CH.BindingType ToNative(PinHandleHolder holder);
}

public readonly struct BindGroupLayoutEntry
{
    private readonly u32 _binding;
    private readonly ShaderStages _visibility;
    private readonly IBindingTypeData _resource;
    private readonly u32 _count;

    [Obsolete("Don't use default constructor.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public BindGroupLayoutEntry() => throw new NotSupportedException("Don't use default constructor.");

    private BindGroupLayoutEntry(u32 binding, ShaderStages visibility, IBindingTypeData bindingData, u32 count)
    {
        _binding = binding;
        _visibility = visibility;
        _resource = bindingData;
        _count = count;
    }

    public static BindGroupLayoutEntry Buffer(u32 binding, ShaderStages visibility, BufferBindingData type)
    {
        return new BindGroupLayoutEntry(binding, visibility, type, 0);
    }

    public static BindGroupLayoutEntry Sampler(u32 binding, ShaderStages visibility, SamplerBindingType type)
    {
        return new BindGroupLayoutEntry(binding, visibility, new SamplerBindingTypeWrap(type), 0);
    }

    public static BindGroupLayoutEntry Texture(u32 binding, ShaderStages visibility, TextureBindingData type)
    {
        return new BindGroupLayoutEntry(binding, visibility, type, 0);
    }

    public static BindGroupLayoutEntry TextureArray(u32 binding, ShaderStages visibility, TextureBindingData type, u32 count)
    {
        return new BindGroupLayoutEntry(binding, visibility, type, count);
    }

    public static BindGroupLayoutEntry StorageTexture(u32 binding, ShaderStages visibility, StorageTextureBindingData type)
    {
        return new BindGroupLayoutEntry(binding, visibility, type, 0);
    }

    private sealed class SamplerBindingTypeWrap : IBindingTypeData
    {
        private CH.SamplerBindingType _ty;

        public SamplerBindingTypeWrap(SamplerBindingType ty)
        {
            _ty = ty.MapOrThrow();
        }

        unsafe CH.BindingType IBindingTypeData.ToNative(PinHandleHolder holder)
        {
            holder.Add(GCHandle.Alloc(this, GCHandleType.Pinned));
            var payload = (CH.SamplerBindingType*)Unsafe.AsPointer(ref _ty);
            return CH.BindingType.Sampler(payload);
        }
    }

    internal CH.BindGroupLayoutEntry ToNative(PinHandleHolder holder)
    {
        return new CH.BindGroupLayoutEntry
        {
            binding = _binding,
            visibility = _visibility.FlagsMap(),
            ty = _resource.ToNative(holder),
            count = _count
        };
    }
}

public sealed class BufferBindingData : IBindingTypeData
{
    private CH.BufferBindingData _nativePayload;
    private BufferBindingType _type;

    public required BufferBindingType Type
    {
        get => _type;
        init
        {
            _nativePayload.ty = value.MapOrThrow();
            _type = value;
        }
    }
    public bool HasDynamicOffset
    {
        get => _nativePayload.has_dynamic_offset;
        init
        {
            _nativePayload.has_dynamic_offset = value;
        }
    }
    public u64? MinBindingSize
    {
        get => _nativePayload.min_binding_size == 0 ? null : _nativePayload.min_binding_size;
        init
        {
            if(value == 0) {
                Throw();
                [DoesNotReturn]
                static void Throw() => throw new ArgumentOutOfRangeException(nameof(value), "value should not be 0");
            }
            _nativePayload.min_binding_size = (value == null) ? 0 : value.Value;
        }
    }

    unsafe CH.BindingType IBindingTypeData.ToNative(PinHandleHolder holder)
    {
        holder.Add(GCHandle.Alloc(this, GCHandleType.Pinned));
        var payload = (CH.BufferBindingData*)Unsafe.AsPointer(ref _nativePayload);
        return CH.BindingType.Buffer(payload);
    }
}

public sealed class TextureBindingData : IBindingTypeData
{
    private CH.TextureBindingData _native;

    private readonly TextureSampleType _sampleType;
    private readonly TextureViewDimension _viewDimension;
    private readonly bool _multisampled;

    public required TextureSampleType SampleType
    {
        get => _sampleType;
        init
        {
            _sampleType = value;
            _native.sample_type = value.MapOrThrow();
        }
    }
    public required TextureViewDimension ViewDimension
    {
        get => _viewDimension;
        init
        {
            _viewDimension = value;
            _native.view_dimension = value.MapOrThrow();
        }
    }
    public required bool Multisampled
    {
        get => _multisampled;
        init
        {
            _multisampled = value;
            _native.multisampled = value;
        }
    }

    unsafe CH.BindingType IBindingTypeData.ToNative(PinHandleHolder holder)
    {
        holder.Add(GCHandle.Alloc(this, GCHandleType.Pinned));
        var payload = (CH.TextureBindingData*)Unsafe.AsPointer(ref _native);
        return CH.BindingType.Texture(payload);
    }
}

public sealed class StorageTextureBindingData : IBindingTypeData
{
    private CH.StorageTextureBindingData _payload;
    private StorageTextureAccess _access;
    private TextureFormat _format;
    private TextureViewDimension _viewDimension;

    public required StorageTextureAccess Access
    {
        get => _access;
        init
        {
            _access = value;
            _payload.access = value.MapOrThrow();
        }
    }

    public TextureFormat Format
    {
        get => _format;
        init
        {
            _format = value;
            _payload.format = value.MapOrThrow();
        }
    }

    public TextureViewDimension ViewDimension
    {
        get => _viewDimension;
        init
        {
            _viewDimension = value;
            _payload.view_dimension = value.MapOrThrow();
        }
    }

    unsafe CH.BindingType IBindingTypeData.ToNative(PinHandleHolder holder)
    {
        holder.Add(GCHandle.Alloc(this, GCHandleType.Pinned));
        var payload = (CH.StorageTextureBindingData*)Unsafe.AsPointer(ref _payload);
        return CH.BindingType.StorageTexture(payload);
    }
}

public enum StorageTextureAccess
{
    [EnumMapTo(CH.StorageTextureAccess.WriteOnly)] WriteOnly,
    [EnumMapTo(CH.StorageTextureAccess.ReadOnly)] ReadOnly,
    [EnumMapTo(CH.StorageTextureAccess.ReadWrite)] ReadWrite,
}
