#nullable enable
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Elffy;

internal readonly struct PinHandleHolder : IDisposable
{
    private readonly List<PinHandle> _handles;
    public PinHandleHolder()
    {
        _handles = new List<PinHandle>();
    }

    public void Add(GCHandle handle)
    {
        _handles.Add(new PinHandle(handle));
    }

    public void Add(MemoryHandle handle)
    {
        _handles.Add(new PinHandle(handle));
    }

    public void Dispose()
    {
        foreach(var handle in _handles) {
            handle.Dispose();
        }
        _handles.Clear();
    }

    private struct PinHandle : IDisposable
    {
        private GCHandle _gcHandle;
        private MemoryHandle _memHandle;

        public PinHandle(GCHandle handle)
        {
            _gcHandle = handle;
            _memHandle = default;
        }

        public PinHandle(MemoryHandle handle)
        {
            _gcHandle = default;
            _memHandle = handle;
        }

        public void Dispose()
        {
            _gcHandle.Free();
            _memHandle.Dispose();
        }
    }
}
