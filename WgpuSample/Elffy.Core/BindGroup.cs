#nullable enable
using Elffy.NativeBind;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy;

public sealed class BindGroup : IEngineManaged
{
    private readonly HostScreen _screen;
    private Rust.OptionBox<Wgpu.BindGroup> _native;
    internal Rust.Ref<Wgpu.BindGroup> NativeRef => _native.Unwrap();

    public HostScreen Screen => _screen;

    public bool IsManaged => _native.IsNone == false;

    private BindGroup(HostScreen screen, Rust.Box<Wgpu.BindGroup> native)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _native = native;
        _screen = screen;
    }

    ~BindGroup() => Release(false);

    private static readonly Action<BindGroup> _release = static self =>
    {
        self.Release(true);
        GC.SuppressFinalize(self);
    };

    private void Release(bool disposing)
    {
        if(InterlockedEx.Exchange(ref _native, Rust.OptionBox<Wgpu.BindGroup>.None).IsSome(out var native)) {
            native.DestroyBindGroup();
            if(disposing) {
            }
        }
    }

    public static Own<BindGroup> Create(HostScreen screen, in BindGroupDescriptor desc)
    {
        using var pins = new PinHandleHolder();
        var bindGroupNative = screen.AsRefChecked().CreateBindGroup(desc.ToNative(pins));
        var bindGroup = new BindGroup(screen, bindGroupNative);
        return Own.RefType(bindGroup, static x => _release(SafeCast.As<BindGroup>(x)));
    }
}

public readonly struct BindGroupDescriptor
{
    public required BindGroupLayout Layout { get; init; }
    public required ReadOnlyMemory<BindGroupEntry> Entries { get; init; }

    internal CE.BindGroupDescriptor ToNative(PinHandleHolder pins)
    {
        return new CE.BindGroupDescriptor
        {
            layout = Layout.NativeRef,
            entries = Entries.SelectToArray(pins, static (x, pins) => x.ToNative(pins)).AsFixedSlice(pins),
        };
    }
}

public readonly struct BindGroupEntry
{
    private readonly u32 _binding;
    private readonly object _resource;

    public u32 Binding => _binding;

    private BindGroupEntry(u32 binding, object resource)
    {
        Debug.Assert(resource is BufferBinding or Elffy.TextureView or Elffy.Sampler);
        _binding = binding;
        _resource = resource;
    }

    internal CE.BindGroupEntry ToNative(PinHandleHolder pins)
    {
        return new CE.BindGroupEntry
        {
            binding = _binding,
            resource = _resource switch
            {
                BufferBinding bufferBinding => bufferBinding.ToNative(pins),
                TextureView textureView => CE.BindingResource.TextureView(textureView.NativeRef),
                Sampler sampler => CE.BindingResource.Sampler(sampler.NativeRef),
                _ => throw new UnreachableException(),
            },
        };
    }

    public static BindGroupEntry Buffer(u32 binding, Buffer buffer) => Buffer(binding, buffer, 0, buffer.ByteLength);

    public static BindGroupEntry Buffer(u32 binding, Buffer buffer, u64 offset, u64 size)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        var resource = new BufferBinding(buffer, offset, size);
        return new BindGroupEntry(binding, resource);
    }

    public static BindGroupEntry TextureView(u32 binding, TextureView textureView)
    {
        ArgumentNullException.ThrowIfNull(textureView);
        return new BindGroupEntry(binding, textureView);
    }

    public static BindGroupEntry Sampler(u32 binding, Sampler sampler)
    {
        ArgumentNullException.ThrowIfNull(sampler);
        return new BindGroupEntry(binding, sampler);
    }

    internal sealed class BufferBinding
    {
        private CE.BufferBinding _native;

        public BufferBinding(Buffer buffer, u64 offset, u64 size)
        {
            _native = new()
            {
                buffer = buffer.NativeRef,
                offset = offset,
                size = size
            };
        }

        internal unsafe CE.BindingResource ToNative(PinHandleHolder pins)
        {
            pins.Add(GCHandle.Alloc(this, GCHandleType.Pinned));
            var payload = (CE.BufferBinding*)Unsafe.AsPointer(ref _native);
            return CE.BindingResource.Buffer(payload);
        }
    }
}
