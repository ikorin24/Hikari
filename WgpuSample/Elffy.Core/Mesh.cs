#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Runtime.InteropServices;

namespace Elffy;

public sealed class Mesh<TVertex>
    : IScreenManaged
    where TVertex : unmanaged, IVertex
{
    private readonly Screen _screen;
    private readonly Own<Buffer> _vertexBuffer;
    private readonly Own<Buffer> _indexBuffer;
    private readonly uint _indexCount;
    private readonly IndexFormat _indexFormat;
    private readonly Own<Buffer> _optTangent;
    private bool _isReleased;

    public BufferSlice VertexBuffer => _vertexBuffer.AsValue().Slice();
    public IndexBufferSlice IndexBuffer => new IndexBufferSlice(_indexFormat, _indexBuffer.AsValue().Slice());

    public uint IndexCount => _indexCount;

    public bool TryGetOptionalTangent(out BufferSlice tangent)
    {
        if(_optTangent.TryAsValue(out var tan)) {
            tangent = tan.Slice();
            return true;
        }
        tangent = default;
        return false;
    }

    public bool HasOptionalTangent => TryGetOptionalTangent(out _);

    public Screen Screen => _screen;

    public bool IsManaged => _isReleased == false;

    private Mesh(Screen screen, Own<Buffer> vertexBuffer, Own<Buffer> indexBuffer, uint indexCount, IndexFormat indexFormat, Own<Buffer> optTangent)
    {
        _screen = screen;
        _vertexBuffer = vertexBuffer;
        _indexBuffer = indexBuffer;
        _indexCount = indexCount;
        _indexFormat = indexFormat;
        _optTangent = optTangent;
        _isReleased = false;
    }

    public void Validate()
    {
        IScreenManaged.DefaultValidate(this);
    }

    private void Release()
    {
        if(_isReleased) {
            return;
        }
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
        _optTangent.Dispose();
        _isReleased = true;
    }

    internal unsafe static Own<Mesh<TVertex>> Create<TIndex>(
        Screen screen,
        TVertex* vertices, u32 vertexLen,
        TIndex* indices, u32 indexLen,
        Vector3* tangents, u32 tangentLen)
        where TIndex : unmanaged
    {
        ArgumentNullException.ThrowIfNull(screen);
        IndexFormat indexFormat;
        if(typeof(TIndex) == typeof(u32)) {
            indexFormat = IndexFormat.Uint32;
        }
        else if(typeof(TIndex) == typeof(u16)) {
            indexFormat = IndexFormat.Uint16;
        }
        else {
            throw new ArgumentException("index type should be u32 or u16.");
        }

        // TODO: pass usage flags from arg
        var vertexBuffer = Buffer.CreateInitBytes(screen, (u8*)vertices, vertexLen * (usize)sizeof(TVertex), BufferUsages.Vertex | BufferUsages.Storage | BufferUsages.CopySrc);
        var indexBuffer = Buffer.CreateInitBytes(screen, (u8*)indices, indexLen * (usize)sizeof(TIndex), BufferUsages.Index | BufferUsages.Storage | BufferUsages.CopySrc);
        Own<Buffer> tangentBuffer;
        if(tangentLen == 0) {
            tangentBuffer = Own<Buffer>.None;
        }
        else {
            tangentBuffer = Buffer.CreateInitBytes(screen, (u8*)tangents, tangentLen * (usize)sizeof(Vector3), BufferUsages.Vertex | BufferUsages.Storage | BufferUsages.CopySrc);
        }

        var mesh = new Mesh<TVertex>(screen, vertexBuffer, indexBuffer, indexLen, indexFormat, tangentBuffer);
        return Own.New(mesh, static x => SafeCast.As<Mesh<TVertex>>(x).Release());
    }
}

public static class Mesh
{
    public unsafe static Own<Mesh<TVertex>> Create<TVertex>(
        Screen screen,
        ReadOnlySpan<TVertex> vertices,
        ReadOnlySpan<u16> indices)
        where TVertex : unmanaged, IVertex
    {
        fixed(TVertex* vp = vertices)
        fixed(u16* ip = indices) {
            return Mesh<TVertex>.Create<u16>(screen, vp, (u32)vertices.Length, ip, (u32)indices.Length, null, 0);
        }
    }

    public unsafe static Own<Mesh<TVertex>> Create<TVertex>(
        Screen screen,
        TVertex* vertices, u32 vertexLen,
        u16* indices, u32 indexLen)
        where TVertex : unmanaged, IVertex
    {
        return Mesh<TVertex>.Create<u16>(screen, vertices, vertexLen, indices, indexLen, null, 0);
    }

    public unsafe static Own<Mesh<TVertex>> Create<TVertex>(
        Screen screen,
        ReadOnlySpan<TVertex> vertices,
        ReadOnlySpan<u32> indices)
        where TVertex : unmanaged, IVertex
    {
        fixed(TVertex* vp = vertices)
        fixed(u32* ip = indices) {
            return Mesh<TVertex>.Create<u32>(screen, vp, (u32)vertices.Length, ip, (u32)indices.Length, null, 0);
        }
    }

    public unsafe static Own<Mesh<TVertex>> Create<TVertex>(
        Screen screen,
        TVertex* vertices, u32 vertexLen,
        u32* indices, u32 indexLen)
        where TVertex : unmanaged, IVertex
    {
        return Mesh<TVertex>.Create<u32>(screen, vertices, vertexLen, indices, indexLen, null, 0);
    }

    public unsafe static Own<Mesh<TVertex>> Create<TVertex>(
        Screen screen,
        ReadOnlySpan<TVertex> vertices,
        ReadOnlySpan<u16> indices,
        ReadOnlySpan<Vector3> tangents)
        where TVertex : unmanaged, IVertex
    {
        fixed(TVertex* v = vertices)
        fixed(u16* i = indices)
        fixed(Vector3* t = tangents) {
            return Mesh<TVertex>.Create<u16>(screen, v, (u32)vertices.Length, i, (u32)indices.Length, t, (u32)tangents.Length);
        }
    }

    public unsafe static Own<Mesh<TVertex>> Create<TVertex>(
        Screen screen,
        TVertex* vertices, u32 vertexLen,
        u16* indices, u32 indexLen,
        Vector3* tangents, u32 tangentLen)
        where TVertex : unmanaged, IVertex
    {
        return Mesh<TVertex>.Create<u16>(screen, vertices, vertexLen, indices, indexLen, tangents, tangentLen);
    }

    public unsafe static Own<Mesh<TVertex>> Create<TVertex>(
        Screen screen,
        ReadOnlySpan<TVertex> vertices,
        ReadOnlySpan<u32> indices,
        ReadOnlySpan<Vector3> tangents)
        where TVertex : unmanaged, IVertex
    {
        fixed(TVertex* v = vertices)
        fixed(u32* i = indices)
        fixed(Vector3* t = tangents) {
            return Mesh<TVertex>.Create<u32>(screen, v, (u32)vertices.Length, i, (u32)indices.Length, t, (u32)tangents.Length);
        }
    }

    public unsafe static Own<Mesh<TVertex>> Create<TVertex>(
        Screen screen,
        TVertex* vertices, u32 vertexLen,
        u32* indices, u32 indexLen,
        Vector3* tangents, u32 tangentLen)
        where TVertex : unmanaged, IVertex
    {
        return Mesh<TVertex>.Create<u32>(screen, vertices, vertexLen, indices, indexLen, tangents, tangentLen);
    }

    public unsafe static Own<Mesh<TVertex>> CreateWithTangent<TVertex>(
        Screen screen,
        ReadOnlySpan<TVertex> vertices,
        ReadOnlySpan<u16> indices)
        where TVertex : unmanaged, IVertex, IVertexUV
    {
        fixed(TVertex* vp = vertices)
        fixed(u16* ip = indices) {
            return CreateWithTangent(screen, vp, (u32)vertices.Length, ip, (u32)indices.Length);
        }
    }

    public unsafe static Own<Mesh<TVertex>> CreateWithTangent<TVertex>(
        Screen screen,
        TVertex* vertices, u32 vertexLen,
        u16* indices, u32 indexLen)
        where TVertex : unmanaged, IVertex, IVertexUV
    {
        var tangentLen = vertexLen;
        var tangents = (Vector3*)NativeMemory.Alloc((usize)sizeof(Vector3), tangentLen);
        try {
            MeshHelper.CalcTangentsU16(vertices, vertexLen, indices, indexLen, tangents);
            return Mesh<TVertex>.Create<u16>(screen, vertices, vertexLen, indices, indexLen, tangents, tangentLen);
        }
        finally {
            NativeMemory.Free(tangents);
        }
    }

    public unsafe static Own<Mesh<TVertex>> CreateWithTangent<TVertex>(
        Screen screen,
        ReadOnlySpan<TVertex> vertices,
        ReadOnlySpan<u32> indices)
        where TVertex : unmanaged, IVertex, IVertexUV
    {
        fixed(TVertex* vp = vertices)
        fixed(u32* ip = indices) {
            return CreateWithTangent(screen, vp, (u32)vertices.Length, ip, (u32)indices.Length);
        }
    }

    public unsafe static Own<Mesh<TVertex>> CreateWithTangent<TVertex>(
        Screen screen,
        TVertex* vertices, u32 vertexLen,
        u32* indices, u32 indexLen)
        where TVertex : unmanaged, IVertex, IVertexUV
    {
        var tangentLen = vertexLen;
        var tangents = (Vector3*)NativeMemory.Alloc((usize)sizeof(Vector3) * tangentLen);
        try {
            MeshHelper.CalcTangentsU32(vertices, vertexLen, indices, indexLen, tangents);
            return Mesh<TVertex>.Create<u32>(screen, vertices, vertexLen, indices, indexLen, tangents, tangentLen);
        }
        finally {
            NativeMemory.Free(tangents);
        }
    }
}

public readonly struct IndexBufferSlice : IReadBuffer
{
    private readonly IndexFormat _format;
    private readonly BufferSlice _byteSlice;

    public IndexBufferSlice(IndexFormat format, BufferSlice byteSlice)
    {
        _format = format;
        _byteSlice = byteSlice;
    }

    public IndexFormat Format => _format;

    public BufferSlice BufferSlice => _byteSlice;

    public UniTask<byte[]> ReadToArray() => _byteSlice.ReadToArray();
    public UniTask<int> Read<TElement>(Memory<TElement> dest) where TElement : unmanaged
        => _byteSlice.Read(dest);
    public void ReadCallback(ReadOnlySpanAction<byte> onRead, Action<Exception>? onException = null) => _byteSlice.ReadCallback(onRead, onException);

    internal CE.BufferSlice BufferSliceNative()
    {
        return _byteSlice.Native();
    }
}
