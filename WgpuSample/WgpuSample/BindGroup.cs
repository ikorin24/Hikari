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
    private BindGroupEntry[]? _entries;

    public IHostScreen? Screen => _screen;

    private BindGroup(IHostScreen screen, Box<Wgpu.BindGroup> native, ReadOnlySpan<BindGroupEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _native = native;
        _screen = screen;
        _entries = entries.ToArray();
    }

    public void Dispose()
    {
        if(_native.IsInvalid) {
            return;
        }
        _native.DestroyBindGroup();
        _native = Box<Wgpu.BindGroup>.Invalid;
        _screen = null;
        _entries = null;
    }

    public static unsafe BindGroup Create(IHostScreen screen, BindGroupLayout layout, ReadOnlySpan<BindGroupEntry> entries)
    {
        // Don't use SkipLocalsInit

        ArgumentNullException.ThrowIfNull(screen);
        ArgumentNullException.ThrowIfNull(layout);
        if(entries.IsEmpty) {
            throw new ArgumentException($"{nameof(entries)} is empty");
        }
        var screenRef = screen.AsRef();
        Span<CE.BindGroupEntry> nativeEntries = stackalloc CE.BindGroupEntry[entries.Length];
        Span<GCHandle?> pinneds = stackalloc GCHandle?[entries.Length];
        try {
            for(int i = 0; i < entries.Length; i++) {
                var entry = entries[i];
                if(entry is null) {
                    throw new ArgumentException($"{nameof(entries)}[{i}] is null");
                }
                nativeEntries[i] = entry.ToNative(out pinneds[i]);
            }
            fixed(CE.BindGroupEntry* e = nativeEntries) {
                var desc = new CE.BindGroupDescriptor
                {
                    layout = layout.NativeRef,
                    entries = new(e, nativeEntries.Length),
                };
                var bindGroup = screenRef.CreateBindGroup(desc);
                return new BindGroup(screen, bindGroup, entries);
            }
        }
        finally {
            foreach(var gcPin in pinneds) {
                if(gcPin.HasValue) {
                    gcPin.Value.Free();
                }
            }
        }
    }
}

public unsafe sealed class BindGroupEntry
{
    private readonly u32 _binding;
    private readonly object _resource;
    private readonly delegate*<BindGroupEntry, out GCHandle?, CE.BindGroupEntry> _toNative;

    public u32 Binding => _binding;

    private BindGroupEntry(u32 binding, object resource, delegate*<BindGroupEntry, out GCHandle?, CE.BindGroupEntry> toNative)
    {
        _binding = binding;
        _resource = resource;
        _toNative = toNative;
    }

    internal CE.BindGroupEntry ToNative(out GCHandle? pinned)
    {
        return _toNative(this, out pinned);
    }

    public static BindGroupEntry Buffer(u32 binding, Buffer buffer, u64 offset, u64 size)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        var resource = new BufferBinding(buffer, offset, size);
        return new BindGroupEntry(binding, resource, &ToNative);

        static CE.BindGroupEntry ToNative(BindGroupEntry self, out GCHandle? pinned)
        {
            var bufferBinding = (BufferBinding)self._resource;
            var payload = bufferBinding.ToNative(out var pinnedHandle);
            pinned = pinnedHandle;

            return new CE.BindGroupEntry
            {
                binding = self.Binding,
                resource = CE.BindingResource.Buffer(payload),
            };
        }
    }

    public static BindGroupEntry TextureView(u32 binding, TextureView textureView)
    {
        ArgumentNullException.ThrowIfNull(textureView);
        return new BindGroupEntry(binding, textureView, &ToNative);

        static CE.BindGroupEntry ToNative(BindGroupEntry self, out GCHandle? pinned)
        {
            var textureView = (TextureView)self._resource;
            pinned = null;
            var payload = textureView.NativeRef;
            payload.ThrowIfInvalid();
            return new CE.BindGroupEntry
            {
                binding = self.Binding,
                resource = CE.BindingResource.TextureView(payload),
            };
        }
    }

    internal sealed class BufferBinding
    {
        private CE.BufferBinding _native;
        //public ref readonly CE.BufferBinding Native => ref _native;

        public BufferBinding(Buffer buffer, u64 offset, u64 size)
        {
            _native = new()
            {
                buffer = buffer.NativeRef,
                offset = offset,
                size = size
            };
        }

        public CE.BufferBinding* ToNative(out GCHandle pinned)
        {
            _native.buffer.ThrowIfInvalid();
            pinned = GCHandle.Alloc(this, GCHandleType.Pinned);
            return (CE.BufferBinding*)Unsafe.AsPointer(ref _native);
        }
    }
}
