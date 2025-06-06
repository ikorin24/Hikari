﻿#nullable enable
using Hikari.Gltf.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Hikari.Gltf.Parsing;

internal sealed class GlbObject : IDisposable
{
    private readonly List<GlbBinaryBuffer> _binaryBuffers;

    private GltfObject _gltf;
    public GltfObject Gltf => _gltf;

    public int BinaryDataCount => _binaryBuffers.Count;

    internal GlbObject(GltfObject gltf)
    {
        _gltf = gltf;
        _binaryBuffers = new List<GlbBinaryBuffer>();
    }

    public unsafe GlbBinaryData GetBinaryData(uint index)
    {
        var buf = _binaryBuffers[(int)index];
        return new GlbBinaryData(buf.Ptr, buf.byteLength);
    }

    internal GlbBinaryBuffer CreateNewBuffer(nuint size)
    {
        var buffer = new GlbBinaryBuffer(size);
        _binaryBuffers.Add(buffer);
        return buffer;
    }

    public void Dispose()
    {
        foreach(var buffer in _binaryBuffers) {
            buffer.Release();
        }
        _binaryBuffers.Clear();
    }
}

internal unsafe struct GlbBinaryData
{
    private byte* _ptr;
    private nuint _length;

    public byte* Ptr => _ptr;
    public nuint ByteLength => _length;

    public GlbBinaryData(byte* ptr, nuint length)
    {
        _ptr = ptr;
        _length = length;
    }

    public GlbBinaryData Slice(nuint offset, nuint length)
    {
        if(offset > _length) {
            ThrowOutOfRange(nameof(offset));
        }
        if(length > _length - offset) {
            ThrowOutOfRange(nameof(length));
        }
        return new GlbBinaryData(_ptr + offset, length);

        [DoesNotReturn] static void ThrowOutOfRange(string name) => throw new ArgumentOutOfRangeException(name);
    }

    public void CopyTo(Span<byte> dest)
    {
        fixed(void* ptr = dest) {
            CopyTo(ptr, (nuint)dest.Length);
        }
    }

    public void CopyTo(void* dest, nuint destByteLength)
    {
        System.Buffer.MemoryCopy(_ptr, dest, destByteLength, _length);
    }
}


internal unsafe sealed class GlbBinaryBuffer
{
    private NativeBuffer _buf;

    public byte* Ptr => _buf.Ptr;
    public nuint byteLength => _buf.ByteLength;

    internal GlbBinaryBuffer(nuint size)
    {
        _buf = new NativeBuffer(size);
    }

    ~GlbBinaryBuffer()
    {
        _buf.Dispose();
    }

    internal void Release()
    {
        GC.SuppressFinalize(this);
        _buf.Dispose();
    }
}
