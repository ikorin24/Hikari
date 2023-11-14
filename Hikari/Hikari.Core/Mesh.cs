#nullable enable
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hikari;

public sealed class Mesh<TVertex>
    : IScreenManaged
    where TVertex : unmanaged, IVertex
{
    private readonly Screen _screen;
    private readonly Own<Buffer> _vertexBuffer;
    private readonly Own<Buffer> _indexBuffer;
    private readonly Own<Buffer> _optTangent;
    private readonly uint _vertexCount;
    private readonly uint _indexCount;
    private readonly IndexFormat _indexFormat;
    private bool _isReleased;

    public BufferSlice VertexBuffer => _vertexBuffer.AsValue().Slice();
    public BufferSlice IndexBuffer => _indexBuffer.AsValue();
    public IndexFormat IndexFormat => _indexFormat;

    public uint IndexCount => _indexCount;
    public uint VertexCount => _vertexCount;

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

    private Mesh(Screen screen, Own<Buffer> vertexBuffer, uint vertexCount, Own<Buffer> indexBuffer, uint indexCount, IndexFormat indexFormat, Own<Buffer> optTangent)
    {
        _screen = screen;
        _vertexBuffer = vertexBuffer;
        _indexBuffer = indexBuffer;
        _vertexCount = vertexCount;
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
        ReadOnlySpanU32<TVertex> vertices,
        ReadOnlySpanU32<TIndex> indices,
        ReadOnlySpanU32<Vector3> tangents,
        BufferUsages usages)
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

        Own<Buffer> vertexBuffer;
        Own<Buffer> indexBuffer;
        fixed(void* v = vertices)
        fixed(void* ind = indices) {
            vertexBuffer = Buffer.CreateInitBytes(screen, (u8*)v, vertices.ByteLength, usages | BufferUsages.Vertex);
            indexBuffer = Buffer.CreateInitBytes(screen, (u8*)ind, indices.ByteLength, usages | BufferUsages.Index);
        }

        Own<Buffer> tangentBuffer;
        if(tangents.IsEmpty) {
            tangentBuffer = Own<Buffer>.None;
        }
        else {
            fixed(void* t = tangents) {
                tangentBuffer = Buffer.CreateInitBytes(screen, (u8*)t, tangents.ByteLength, usages | BufferUsages.Vertex);
            }
        }

        var mesh = new Mesh<TVertex>(screen, vertexBuffer, vertices.Length, indexBuffer, indices.Length, indexFormat, tangentBuffer);
        return Own.New(mesh, static x => SafeCast.As<Mesh<TVertex>>(x).Release());
    }
}

public static class Mesh
{
    private const BufferUsages DefaultUsages = BufferUsages.Storage | BufferUsages.CopySrc;

    public static Own<Mesh<TVertex>> Create<TVertex, TIndex>(
        Screen screen,
        ReadOnlySpanU32<TVertex> vertices,
        ReadOnlySpanU32<TIndex> indices,
        BufferUsages usages = DefaultUsages)
        where TVertex : unmanaged, IVertex
        where TIndex : unmanaged, INumberBase<TIndex>
    {
        return Mesh<TVertex>.Create(screen, vertices, indices, ReadOnlySpanU32<Vector3>.Empty, usages);
    }

    public unsafe static Own<Mesh<TVertex>> CreateWithTangent<TVertex, TIndex>(
        Screen screen,
        ReadOnlySpanU32<TVertex> vertices,
        ReadOnlySpanU32<TIndex> indices,
        BufferUsages usages = DefaultUsages)
        where TVertex : unmanaged, IVertex, IVertexUV
        where TIndex : unmanaged, INumberBase<TIndex>
    {
        var tangentLen = vertices.Length;
        var tp = NativeMemory.Alloc((usize)Unsafe.SizeOf<Vector3>() * tangentLen);
        try {
            var tangents = new SpanU32<Vector3>(tp, tangentLen);
            MeshHelper.CalcTangent(vertices, indices, tangents);
            return Mesh<TVertex>.Create(screen, vertices, indices, tangents, usages);
        }
        finally {
            NativeMemory.Free(tp);
        }
    }

    public static Own<Mesh<TVertex>> CreateWithTangent<TVertex, TIndex>(
        Screen screen,
        ReadOnlySpanU32<TVertex> vertices,
        ReadOnlySpanU32<TIndex> indices,
        ReadOnlySpanU32<Vector3> tangents,
        BufferUsages usages = DefaultUsages)
        where TVertex : unmanaged, IVertex
        where TIndex : unmanaged, INumberBase<TIndex>
    {
        return Mesh<TVertex>.Create(screen, vertices, indices, tangents, usages);
    }
}
