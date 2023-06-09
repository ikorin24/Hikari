#nullable enable
using Elffy.NativeBind;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy;

public sealed class BindGroup : IScreenManaged
{
    private readonly Screen _screen;
    private Rust.OptionBox<Wgpu.BindGroup> _native;
    private readonly IScreenManaged[] _associated;

    internal Rust.Ref<Wgpu.BindGroup> NativeRef => _native.Unwrap();

    public Screen Screen => _screen;

    public bool IsManaged => _native.IsNone == false;

    public void Validate()
    {
        IScreenManaged.DefaultValidate(this);
        foreach(var item in _associated) {
            item.Validate();
        }
    }

    private BindGroup(Screen screen, Rust.Box<Wgpu.BindGroup> native, IScreenManaged[] associated)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _native = native;
        _screen = screen;
        _associated = associated;
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

    public static Own<BindGroup> Create(Screen screen, in BindGroupDescriptor desc)
    {
        using var pins = new PinHandleHolder();
        var bindGroupNative = screen.AsRefChecked().CreateBindGroup(desc.ToNative(pins));

        var entries = desc.Entries.Span;
        var associated = new IScreenManaged[1 + entries.Length];
        for(int i = 0; i < entries.Length; i++) {
            associated[i] = entries[i].Resource;
        }
        associated[entries.Length] = desc.Layout;
        var bindGroup = new BindGroup(screen, bindGroupNative, associated);

        return Own.New(bindGroup, static x => _release(SafeCast.As<BindGroup>(x)));
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
    private readonly IScreenManaged _resource;

    public u32 Binding => _binding;

    internal IScreenManaged Resource => _resource;

    private BindGroupEntry(u32 binding, IScreenManaged resource)
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

        public BufferBinding(BufferSlice bufferSlice) : this(bufferSlice.Buffer, bufferSlice.StartByteOffset, bufferSlice.ByteLength)
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

        internal unsafe CE.BindingResource ToNative(PinHandleHolder pins)
        {
            pins.Add(GCHandle.Alloc(_wrap, GCHandleType.Pinned));
            var payload = (CE.BufferBinding*)Unsafe.AsPointer(ref Unsafe.AsRef(in _wrap.Native));
            return CE.BindingResource.Buffer(payload);
        }

        private sealed class Wrap
        {
            public readonly CE.BufferBinding Native;
            public Wrap(CE.BufferBinding native)
            {
                Native = native;
            }
        }
    }
}
