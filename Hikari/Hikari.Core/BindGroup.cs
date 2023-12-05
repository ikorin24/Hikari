#nullable enable
using Hikari.NativeBind;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hikari;

public sealed partial class BindGroup : IScreenManaged
{
    private readonly Screen _screen;
    private Rust.OptionBox<Wgpu.BindGroup> _native;
    private readonly BindGroupDescriptor _desc;

    internal Rust.Ref<Wgpu.BindGroup> NativeRef => _native.Unwrap();

    public Screen Screen => _screen;

    public bool IsManaged => _native.IsNone == false;

    public BindGroupLayout Layout => _desc.Layout;
    public ImmutableArray<BindGroupEntry> Entries => _desc.Entries;

    public void Validate()
    {
        IScreenManaged.DefaultValidate(this);
        _desc.Layout.Validate();
        foreach(var entry in _desc.Entries) {
            entry.Resource.Validate();
        }
    }

    [Owned(nameof(Release))]
    private BindGroup(Screen screen, in BindGroupDescriptor desc)
    {
        ArgumentNullException.ThrowIfNull(screen);
        using var pins = new PinHandleHolder();
        _native = screen.AsRefChecked().CreateBindGroup(desc.ToNative(pins));
        _screen = screen;
        _desc = desc;
    }

    ~BindGroup() => Release(false);

    private void Release()
    {
        Release(true);
        GC.SuppressFinalize(this);
    }

    private void Release(bool disposing)
    {
        if(InterlockedEx.Exchange(ref _native, Rust.OptionBox<Wgpu.BindGroup>.None).IsSome(out var native)) {
            native.DestroyBindGroup();
            if(disposing) {
            }
        }
    }
}

public readonly struct BindGroupDescriptor
{
    public required BindGroupLayout Layout { get; init; }
    public required ImmutableArray<BindGroupEntry> Entries { get; init; }

    internal CH.BindGroupDescriptor ToNative(PinHandleHolder pins)
    {
        return new CH.BindGroupDescriptor
        {
            layout = Layout.NativeRef,
            entries = Entries.SelectToArray(pins, static (x, pins) => x.ToNative(pins)).AsFixedSlice(pins),
        };
    }
}

public readonly struct BindGroupEntry
{
    private readonly u32 _binding;
    private readonly IScreenManaged _resource;

    public u32 Binding => _binding;

    internal IScreenManaged Resource => _resource;

    private BindGroupEntry(u32 binding, IScreenManaged resource)
    {
        Debug.Assert(resource is BufferBinding or Hikari.TextureView or Hikari.Sampler);
        _binding = binding;
        _resource = resource;
    }

    internal CH.BindGroupEntry ToNative(PinHandleHolder pins)
    {
        return new CH.BindGroupEntry
        {
            binding = _binding,
            resource = _resource switch
            {
                BufferBinding bufferBinding => bufferBinding.ToNative(pins),
                TextureView textureView => CH.BindingResource.TextureView(textureView.NativeRef),
                Sampler sampler => CH.BindingResource.Sampler(sampler.NativeRef),
                _ => throw new UnreachableException(),
            },
        };
    }

    public static BindGroupEntry Buffer<T>(u32 binding, CachedOwnBuffer<T> buffer)
        where T : unmanaged
        => Buffer(binding, buffer.AsBuffer());

    internal static BindGroupEntry Buffer<T>(u32 binding, TypedOwnBuffer<T> buffer)
        where T : unmanaged
        => Buffer(binding, buffer.AsBuffer());

    public static BindGroupEntry Buffer(u32 binding, Buffer buffer) => Buffer(binding, buffer, 0, buffer.ByteLength);

    public static BindGroupEntry Buffer(u32 binding, Buffer buffer, u64 offset, u64 size)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        var resource = new BufferBinding(buffer, offset, size);
        return new BindGroupEntry(binding, resource);
    }

    public static BindGroupEntry Buffer(u32 binding, BufferSlice bufferSlice)
    {
        var resource = new BufferBinding(bufferSlice);
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

    internal sealed class BufferBinding : IScreenManaged
    {
        private readonly Buffer _buffer;
        private readonly Wrap _wrap;

        public Screen Screen => _buffer.Screen;

        public bool IsManaged => _buffer.IsManaged;

        public BufferBinding(BufferSlice bufferSlice) : this(bufferSlice.Buffer, bufferSlice.Offset, bufferSlice.Length)
        {
        }

        public BufferBinding(Buffer buffer, u64 offset, u64 size)
        {
            _buffer = buffer;
            _wrap = new Wrap(new()
            {
                buffer = buffer.NativeRef,
                offset = offset,
                size = size
            });
        }

        public void Validate() => IScreenManaged.DefaultValidate(this);

        internal unsafe CH.BindingResource ToNative(PinHandleHolder pins)
        {
            _buffer.Validate();
            pins.Add(GCHandle.Alloc(_wrap, GCHandleType.Pinned));
            var payload = (CH.BufferBinding*)Unsafe.AsPointer(ref Unsafe.AsRef(in _wrap.Native));
            return CH.BindingResource.Buffer(payload);
        }

        private sealed class Wrap
        {
            public readonly CH.BufferBinding Native;
            public Wrap(CH.BufferBinding native)
            {
                Native = native;
            }
        }
    }
}
