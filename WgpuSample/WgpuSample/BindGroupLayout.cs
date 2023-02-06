#nullable enable
using Elffy.Bind;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy;

public sealed class BindGroupLayout : IEngineManaged, IDisposable
{
    private IHostScreen? _screen;
    private Box<Wgpu.BindGroupLayout> _native;
    private BindGroupLayoutEntry[]? _entries;

    public IHostScreen? Screen => _screen;

    internal Ref<Wgpu.BindGroupLayout> NativeRef => _native;

    private BindGroupLayout(IHostScreen screen, Box<Wgpu.BindGroupLayout> native, ReadOnlySpan<BindGroupLayoutEntry> entries)
    {
        _screen = screen;
        _native = native;
        _entries = entries.ToArray();
    }

    public unsafe static BindGroupLayout Create(IHostScreen screen, ReadOnlySpan<BindGroupLayoutEntry> entries)
    {
        // Don't use SkipLocalsInit

        ArgumentNullException.ThrowIfNull(screen);
        if(entries.IsEmpty) {
            throw new ArgumentException($"{nameof(entries)} is empty");
        }
        var screenRef = screen.AsRef();
        Span<CE.BindGroupLayoutEntry> nativeEntries = stackalloc CE.BindGroupLayoutEntry[entries.Length];
        Span<GCHandle?> pinneds = stackalloc GCHandle?[entries.Length];
        try {
            for(int i = 0; i < entries.Length; i++) {
                var entry = entries[i];
                if(entry is null) {
                    throw new ArgumentException($"{nameof(entries)}[{i}] is null");
                }
                nativeEntries[i] = entry.ToNative(out pinneds[i]);
            }
            fixed(CE.BindGroupLayoutEntry* e = nativeEntries) {
                var desc = new CE.BindGroupLayoutDescriptor
                {
                    entries = new(e, entries.Length),
                };
                var bindGroupLayout = screenRef.CreateBindGroupLayout(desc);
                return new BindGroupLayout(screen, bindGroupLayout, entries);
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

    public void Dispose()
    {
        if(_native.IsInvalid) {
            return;
        }
        _native.DestroyBindGroupLayout();
        _native = Box<Wgpu.BindGroupLayout>.Invalid;
        _screen = null;
        _entries = null;
    }
}

public unsafe sealed class BindGroupLayoutEntry
{
    private readonly u32 _binding;
    private readonly ShaderStages _visibility;
    private readonly u32 _count;
    private readonly object _resource;
    private readonly delegate*<BindGroupLayoutEntry, out GCHandle?, CE.BindGroupLayoutEntry> _toNative;

    private BindGroupLayoutEntry(u32 binding, ShaderStages visibility, uint count, object resource, delegate*<BindGroupLayoutEntry, out GCHandle?, CE.BindGroupLayoutEntry> toNative)
    {
        _binding = binding;
        _visibility = visibility;
        _count = count;
        _resource = resource;
        _toNative = toNative;
    }

    internal CE.BindGroupLayoutEntry ToNative(out GCHandle? pinned)
    {
        return _toNative(this, out pinned);
    }

    public static BindGroupLayoutEntry Buffer(
        u32 binding,
        u32 count,
        ShaderStages visibility,
        BufferBindingType bufferBindingType,
        bool hasDynamicOffset = false,
        u64 minBindingSize = 0)
    {
        bufferBindingType.TryMapTo(out CE.BufferBindingType bufferBindingTypeNative).WithDebugAssertTrue();
        var bufferBindingData = new ObjectWrap<CE.BufferBindingData>(new()
        {
            ty = bufferBindingTypeNative,
            has_dynamic_offset = hasDynamicOffset,
            min_binding_size = minBindingSize,
        });
        return new BindGroupLayoutEntry(binding, visibility, count, bufferBindingData, &ToNative);

        static CE.BindGroupLayoutEntry ToNative(BindGroupLayoutEntry self, out GCHandle? pinned)
        {
            var bufferBindingData = (ObjectWrap<CE.BufferBindingData>)self._resource;
            self._visibility.TryMapTo(out Wgpu.ShaderStages vis).WithDebugAssertTrue();
            var payload = bufferBindingData.GetPointerToValue(out pinned);
            return new CE.BindGroupLayoutEntry
            {
                binding = self._binding,
                count = self._count,
                visibility = vis,
                ty = CE.BindingType.Buffer(payload),
            };
        }
    }

    private sealed class ObjectWrap<T> where T : unmanaged
    {
        private T _value;
        public ObjectWrap(T value)
        {
            _value = value;
        }

        public T* GetPointerToValue(out GCHandle? pinned)
        {
            pinned = GCHandle.Alloc(this, GCHandleType.Pinned);
            return (T*)Unsafe.AsPointer(ref _value);
        }
    }
}
