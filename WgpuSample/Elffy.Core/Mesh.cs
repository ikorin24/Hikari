#nullable enable
using System;
using System.Runtime.CompilerServices;

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
    private bool _isReleased;

    public BufferSlice<TVertex> VertexBuffer => _vertexBuffer.AsValue().Slice<TVertex>();
    public BufferSlice IndexBuffer => _indexBuffer.AsValue().Slice();
    public BufferSlice<u32> IndexBufferU32
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if(_indexFormat != IndexFormat.Uint32) {
                throw new InvalidOperationException("index format is not uint32");
            }
            return IndexBuffer.OfType<u32>();
        }
    }

    public BufferSlice<u16> IndexBufferU16
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if(_indexFormat != IndexFormat.Uint16) {
                throw new InvalidOperationException("index format is not uint16");
            }
            return IndexBuffer.OfType<u16>();
        }
    }
    public IndexFormat IndexFormat => _indexFormat;
    public uint IndexCount => _indexCount;

    public bool HasPosition => typeof(TVertex).IsAssignableTo(typeof(IVertexPosition));
    public bool HasUV => typeof(TVertex).IsAssignableTo(typeof(IVertexUV));
    public bool HasNormal => typeof(TVertex).IsAssignableTo(typeof(IVertexNormal));
    public bool HasColor => typeof(TVertex).IsAssignableTo(typeof(IVertexColor));
    public bool HasTextureIndex => typeof(TVertex).IsAssignableTo(typeof(IVertexTextureIndex));
    public bool HasBone => typeof(TVertex).IsAssignableTo(typeof(IVertexBone));
    public bool HasWeight => typeof(TVertex).IsAssignableTo(typeof(IVertexWeight));
    public bool HasTangent => typeof(TVertex).IsAssignableTo(typeof(IVertexTangent));

    public Screen Screen => _screen;

    public bool IsManaged => _isReleased == false;

    private Mesh(Screen screen, Own<Buffer> vertexBuffer, Own<Buffer> indexBuffer, uint indexCount, IndexFormat indexFormat)
    {
        _screen = screen;
        _vertexBuffer = vertexBuffer;
        _indexBuffer = indexBuffer;
        _indexCount = indexCount;
        _indexFormat = indexFormat;
    }

    private void Release()
    {
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
        _isReleased = true;
    }

    public static Own<Mesh<TVertex>> Create(Screen screen, ReadOnlySpan<TVertex> vertices, ReadOnlySpan<u16> indices)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var vb = Buffer.CreateVertexBuffer(screen, vertices);
        var ib = Buffer.CreateIndexBuffer(screen, indices);
        return Create(screen, vb, ib, (u32)indices.Length, IndexFormat.Uint16);
    }

    public static Own<Mesh<TVertex>> Create(Screen screen, ReadOnlySpan<TVertex> vertices, ReadOnlySpan<u32> indices)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var vb = Buffer.CreateVertexBuffer(screen, vertices);
        var ib = Buffer.CreateIndexBuffer(screen, indices);
        return Create(screen, vb, ib, (u32)indices.Length, IndexFormat.Uint32);
    }

    private static Own<Mesh<TVertex>> Create(Screen screen, Own<Buffer> vertexBuffer, Own<Buffer> indexBuffer, uint indexCount, IndexFormat indexFormat)
    {
        var mesh = new Mesh<TVertex>(screen, vertexBuffer, indexBuffer, indexCount, indexFormat);
        return Own.RefType(mesh, static x => SafeCast.As<Mesh<TVertex>>(x).Release());
    }
}

internal static class MeshHelper
{
    private unsafe static void CalcTangents<TVertex>(TVertex* vertices, u32 verticesLen, u32* indices, u64 indicesLen, Vector3* tangents)
        where TVertex : unmanaged, IVertex, IVertexPosition, IVertexUV
    {
        // TODO:

        Vector3u* triangles = (Vector3u*)indices;
        u64 trianglesLen = indicesLen / 3;
        for(u64 i = 0; i < trianglesLen; i++) {
            var (i0, i1, i2) = triangles[i];
            ref readonly var p0 = ref VertexAccessor.Position(vertices[i0]);
            ref readonly var uv0 = ref VertexAccessor.UV(vertices[i0]);
            ref readonly var p1 = ref VertexAccessor.Position(vertices[i1]);
            ref readonly var uv1 = ref VertexAccessor.UV(vertices[i1]);
            ref readonly var p2 = ref VertexAccessor.Position(vertices[i2]);
            ref readonly var uv2 = ref VertexAccessor.UV(vertices[i2]);
        }

        throw new NotImplementedException();
    }
}

public record struct MeshOptions(bool CalcTangentIfNeeded);
