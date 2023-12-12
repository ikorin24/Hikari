#nullable enable
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Hikari;

public sealed partial class Mesh
{
    private readonly Screen _screen;
    private readonly MeshData _data;
    private readonly ImmutableArray<SubmeshData> _submeshes;
    private bool _isReleased;

    public BufferSlice VertexBuffer => _data.VertexBuffer.AsValue();
    public BufferSlice IndexBuffer => _data.IndexBuffer.AsValue();
    public BufferSlice? TangentBuffer => _data.OptTangentBuffer.TryAsValue(out var buf) ? buf.Slice() : (BufferSlice?)null;

    public ImmutableArray<VertexSlotData> VertexSlots => _data.VertexSlots;
    public ReadOnlySpan<SubmeshData> Submeshes => _submeshes.AsSpan();

    public IndexFormat IndexFormat => _data.IndexFormat;

    public uint IndexCount => _data.IndexCount;
    public uint VertexCount => _data.VertexCount;

    public Screen Screen => _screen;

    private Mesh(Screen screen, in MeshData data, ImmutableArray<SubmeshData> submeshes)
    {
        Debug.Assert(submeshes.IsEmpty == false);
        _screen = screen;
        _data = data;
        _submeshes = submeshes;
        _isReleased = false;
    }

    private void Release()
    {
        if(_isReleased) {
            return;
        }
        _data.VertexBuffer.Dispose();
        _data.IndexBuffer.Dispose();
        _data.OptTangentBuffer.Dispose();
        _isReleased = true;
    }

    private readonly record struct MeshData
    {
        public required Own<Buffer> VertexBuffer { get; init; }
        public required uint VertexCount { get; init; }
        public required Own<Buffer> IndexBuffer { get; init; }
        public required uint IndexCount { get; init; }
        public required IndexFormat IndexFormat { get; init; }
        public required Own<Buffer> OptTangentBuffer { get; init; }
        public required ImmutableArray<VertexSlotData> VertexSlots { get; init; }
    }
}

public readonly ref struct MeshDescriptor<TVertex, TIndex>
    where TVertex : unmanaged, IVertex
    where TIndex : unmanaged, INumberBase<TIndex>
{
    private static readonly ImmutableArray<SubmeshData> _empty = [];

    public required MeshBufferDataDescriptor<TVertex> Vertices { get; init; }
    public required MeshBufferDataDescriptor<TIndex> Indices { get; init; }
    public MeshBufferDataDescriptor<Vector3> Tangents { get; init; }
    public ImmutableArray<SubmeshData> Submeshes { get; init; } = _empty;

    public MeshDescriptor()
    {
    }
}

public readonly record struct SubmeshData
{
    public required int VertexOffset { get; init; }
    public required uint IndexOffset { get; init; }
    public required uint IndexCount { get; init; }

    [SetsRequiredMembers]
    public SubmeshData(int vertexOffset, uint indexOffset, uint indexCount)
    {
        VertexOffset = vertexOffset;
        IndexOffset = indexOffset;
        IndexCount = indexCount;
    }
}

public readonly ref struct MeshBufferDataDescriptor<T>
    where T : unmanaged
{
    public static MeshBufferDataDescriptor<T> None => default;

    public required ReadOnlySpanU32<T> Data { get; init; }
    public required BufferUsages Usages { get; init; }
}

public readonly record struct VertexSlotData
{
    public required uint Slot { get; init; }
    public required BufferSlice Vertices { get; init; }

    [SetsRequiredMembers]
    public VertexSlotData(uint slot, BufferSlice vertices)
    {
        Slot = slot;
        Vertices = vertices;
    }

    public void Deconstruct(out uint slot, out BufferSlice vertices)
    {
        slot = Slot;
        vertices = Vertices;
    }
}
