#nullable enable
using System;

namespace Elffy;

public sealed class Mesh
{
    private Own<Buffer> _vertexBuffer;
    private Own<Buffer> _indexBuffer;
    private uint _indexCount;
    private IndexFormat _indexFormat;

    public Buffer VertexBuffer => _vertexBuffer.AsValue();
    public Buffer IndexBuffer => _indexBuffer.AsValue();
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

    public static Own<Mesh> Create(IHostScreen screen, Own<Buffer> vertexBuffer, Own<Buffer> indexBuffer, uint indexCount, IndexFormat indexFormat)
    {
        ArgumentNullException.ThrowIfNull(screen);
        vertexBuffer.ThrowArgumentExceptionIfNone();
        indexBuffer.ThrowArgumentExceptionIfNone();
        var mesh = new Mesh(vertexBuffer, indexBuffer, indexCount, indexFormat);
        return Own.RefType(mesh, static x => SafeCast.As<Mesh>(x).Release());
    }
}
