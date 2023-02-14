#nullable enable
using Elffy.NativeBind;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy;

public sealed class BindGroupLayout : IEngineManaged
{
    private IHostScreen? _screen;
    private Box<Wgpu.BindGroupLayout> _native;

    public IHostScreen? Screen => _screen;

    internal Ref<Wgpu.BindGroupLayout> NativeRef => _native;

    private BindGroupLayout(IHostScreen screen, Box<Wgpu.BindGroupLayout> native)
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
        var native = Box.SwapClear(ref _native);
        if(native.IsInvalid) {
            return;
        }
        native.DestroyBindGroupLayout();
        if(disposing) {
            _screen = null;
        }
    }

    public unsafe static Own<BindGroupLayout> Create(IHostScreen screen, in BindGroupLayoutDescriptor desc)
    {
        using var pins = new PinHandleHolder();
        var descNative = desc.ToNative(pins);
        var bindGroupLayout = screen.AsRefChecked().CreateBindGroupLayout(descNative);
        return Own.New(new BindGroupLayout(screen, bindGroupLayout), _release);
    }
}

public readonly struct BindGroupLayoutDescriptor
{
    public required ReadOnlyMemory<BindGroupLayoutEntry> Entries { get; init; }

    internal unsafe CE.BindGroupLayoutDescriptor ToNative(PinHandleHolder pins)
    {
        return new CE.BindGroupLayoutDescriptor
        {
            entries = Entries.SelectToArray(pins, static (entry, pins) => entry.ToNative(pins)).AsFixedSlice(pins),
        };
    }
}

internal interface IBindingTypeData
{
    CE.BindingType ToNative(PinHandleHolder holder);
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

    public static BindGroupLayoutEntry Buffer(u32 binding, ShaderStages visibility, BufferBindingData type, u32 count)
    {
        return new BindGroupLayoutEntry(binding, visibility, type, count);
    }

    public static BindGroupLayoutEntry Sampler(u32 binding, ShaderStages visibility, SamplerBindingType type, u32 count)
    {
        return new BindGroupLayoutEntry(binding, visibility, new SamplerBindingTypeWrap(type), count);
    }

    public static BindGroupLayoutEntry Texture(u32 binding, ShaderStages visibility, TextureBindingData type, u32 count)
    {
        return new BindGroupLayoutEntry(binding, visibility, type, count);
    }

    private sealed class SamplerBindingTypeWrap : IBindingTypeData
    {
        private CE.SamplerBindingType _ty;

        public SamplerBindingTypeWrap(SamplerBindingType ty)
        {
            _ty = ty.MapOrThrow();
        }

        unsafe CE.BindingType IBindingTypeData.ToNative(PinHandleHolder holder)
        {
            holder.Add(GCHandle.Alloc(this, GCHandleType.Pinned));
            var payload = (CE.SamplerBindingType*)Unsafe.AsPointer(ref _ty);
            return CE.BindingType.Sampler(payload);
        }
    }

    internal CE.BindGroupLayoutEntry ToNative(PinHandleHolder holder)
    {
        return new CE.BindGroupLayoutEntry
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
    private CE.BufferBindingData _nativePayload;
    private BufferBindingType _type;
    private bool _hasDynamicOffset;
    private u64 _minBindingSize;

    public required BufferBindingType Type
    {
        get => _type;
        init
        {
            _nativePayload.ty = value.MapOrThrow();
            _type = value;
        }
    }
    public required bool HasDynamicOffset
    {
        get => _hasDynamicOffset;
        init
        {
            _nativePayload.has_dynamic_offset = value;
            _hasDynamicOffset = value;
        }
    }
    public required u64 MinBindingSize
    {
        get => _minBindingSize;
        set
        {
            _nativePayload.min_binding_size = value;
            _minBindingSize = value;
        }
    }

    unsafe CE.BindingType IBindingTypeData.ToNative(PinHandleHolder holder)
    {
        holder.Add(GCHandle.Alloc(this, GCHandleType.Pinned));
        var payload = (CE.BufferBindingData*)Unsafe.AsPointer(ref _nativePayload);
        return CE.BindingType.Buffer(payload);
    }
}

public sealed class TextureBindingData : IBindingTypeData
{
    private CE.TextureBindingData _native;

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

    unsafe CE.BindingType IBindingTypeData.ToNative(PinHandleHolder holder)
    {
        holder.Add(GCHandle.Alloc(this, GCHandleType.Pinned));
        var payload = (CE.TextureBindingData*)Unsafe.AsPointer(ref _native);
        return CE.BindingType.Texture(payload);
    }
}
