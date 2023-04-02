#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace Elffy;

public sealed class Mesh<TVertex>
    where TVertex : unmanaged, IVertex
{
    private readonly Own<Buffer> _vertexBuffer;
    private readonly Own<Buffer> _indexBuffer;
    private readonly uint _indexCount;
    private readonly IndexFormat _indexFormat;

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

    private Mesh(Own<Buffer> vertexBuffer, Own<Buffer> indexBuffer, uint indexCount, IndexFormat indexFormat)
    {
        _vertexBuffer = vertexBuffer;
        _indexBuffer = indexBuffer;
        _indexCount = indexCount;
        _indexFormat = indexFormat;
    }

    private void Release()
    {
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
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
        var mesh = new Mesh<TVertex>(vertexBuffer, indexBuffer, indexCount, indexFormat);
        return Own.RefType(mesh, static x => SafeCast.As<Mesh<TVertex>>(x).Release());
    }
}
