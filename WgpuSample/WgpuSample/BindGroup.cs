#nullable enable
using Elffy.Bind;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy;

public sealed class BindGroup : IEngineManaged, IDisposable
{
    private IHostScreen? _screen;
    private Box<Wgpu.BindGroup> _native;

    public IHostScreen? Screen => _screen;

    private BindGroup(IHostScreen screen, Box<Wgpu.BindGroup> native)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _native = native;
        _screen = screen;
    }

    public void Dispose()
    {
        if(_native.IsInvalid) {
            return;
        }
        _native.DestroyBindGroup();
        _native = Box<Wgpu.BindGroup>.Invalid;
        _screen = null;
    }

    public static unsafe BindGroup Create(IHostScreen screen, in BindGroupDescriptor desc)
    {
        using var pins = new PinHandleHolder();
        var bindGroup = screen.AsRef().CreateBindGroup(desc.ToNative(pins));
        return new BindGroup(screen, bindGroup);
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
            entries = Entries.SelectToArray(x => x.ToNative(pins)).AsFixedSlice(pins),
        };
    }
}

public unsafe sealed class BindGroupEntry
{
    private readonly u32 _binding;
    private readonly object _resource;
    private readonly delegate*<object, PinHandleHolder, CE.BindingResource> _resourceToNative;

    public u32 Binding => _binding;

    private BindGroupEntry(u32 binding, object resource, delegate*<object, PinHandleHolder, CE.BindingResource> resourceToNative)
    {
        _binding = binding;
        _resource = resource;
        _resourceToNative = resourceToNative;
    }

    internal CE.BindGroupEntry ToNative(PinHandleHolder pins)
    {
        return new CE.BindGroupEntry
        {
            binding = _binding,
            resource = _resourceToNative(_resource, pins),
        };
    }

    public static BindGroupEntry Buffer(u32 binding, Buffer buffer, u64 offset, u64 size)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        var resource = new BufferBinding(buffer, offset, size);
        return new BindGroupEntry(binding, resource, &ResourceToNative);

        static CE.BindingResource ResourceToNative(object resource, PinHandleHolder pins)
        {
            return ((BufferBinding)resource).ToNative(pins);
        }
    }

    public static BindGroupEntry TextureView(u32 binding, TextureView textureView)
    {
        ArgumentNullException.ThrowIfNull(textureView);
        return new BindGroupEntry(binding, textureView, &ResourceToNative);

        static CE.BindingResource ResourceToNative(object resource, PinHandleHolder pins)
        {
            return CE.BindingResource.TextureView(((TextureView)resource).NativeRef);
        }
    }

    public static BindGroupEntry Sampler(u32 binding, Sampler sampler)
    {
        ArgumentNullException.ThrowIfNull(sampler);
        return new BindGroupEntry(binding, sampler, &ResourceToNative);

        static CE.BindingResource ResourceToNative(object resource, PinHandleHolder pins)
        {
            return CE.BindingResource.Sampler(((Sampler)resource).NativeRef);
        }
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

        internal CE.BindingResource ToNative(PinHandleHolder pins)
        {
            pins.Add(GCHandle.Alloc(this, GCHandleType.Pinned));
            var payload = (CE.BufferBinding*)Unsafe.AsPointer(ref _native);
            return CE.BindingResource.Buffer(payload);
        }
    }
}
