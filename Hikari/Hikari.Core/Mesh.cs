#nullable enable
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hikari;

public sealed partial class Mesh : IScreenManaged
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

    public bool IsManaged => _isReleased == false;

    private Mesh(Screen screen, in MeshData data, ImmutableArray<SubmeshData> submeshes)
    {
        Debug.Assert(submeshes.IsEmpty == false);
        _screen = screen;
        _data = data;
        _submeshes = submeshes;
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

partial class Mesh
{
    private const BufferUsages DefaultUsages = BufferUsages.Storage | BufferUsages.CopySrc;

    public unsafe static Own<Mesh> Create<TVertex, TIndex>(Screen screen, in MeshDescriptor<TVertex, TIndex> desc)
        where TVertex : unmanaged, IVertex
        where TIndex : unmanaged, INumberBase<TIndex>
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
            throw new ArgumentException("index type should be uint or ushort.");
        }
        Own<Buffer> vertexBuffer;
        Own<Buffer> indexBuffer;
        fixed(void* v = desc.Vertices.Data)
        fixed(void* ind = desc.Indices.Data) {
            vertexBuffer = Buffer.CreateInitBytes(screen, (u8*)v, desc.Vertices.Data.ByteLength, desc.Vertices.Usages | BufferUsages.Vertex);
            indexBuffer = Buffer.CreateInitBytes(screen, (u8*)ind, desc.Indices.Data.ByteLength, desc.Indices.Usages | BufferUsages.Index);
        }
        Own<Buffer> tangentBuffer;
        if(desc.Tangents.Data.IsEmpty) {
            tangentBuffer = Own<Buffer>.None;
        }
        else {
            fixed(void* t = desc.Tangents.Data) {
                tangentBuffer = Buffer.CreateInitBytes(screen, (u8*)t, desc.Tangents.Data.ByteLength, desc.Tangents.Usages | BufferUsages.Vertex);
            }
        }
        var meshData = new MeshData
        {
            VertexBuffer = vertexBuffer,
            VertexCount = desc.Vertices.Data.Length,
            IndexBuffer = indexBuffer,
            IndexCount = desc.Indices.Data.Length,
            IndexFormat = indexFormat,
            OptTangentBuffer = tangentBuffer,
            VertexSlots = tangentBuffer switch
            {
                { IsNone: true } => [
                    new(0, vertexBuffer.AsValue().Slice()),
                ],
                _ => [
                    new(0, vertexBuffer.AsValue().Slice()),
                    new(1, tangentBuffer.AsValue().Slice()),
                ],
            },
        };
        var submeshes = desc.Submeshes switch
        {
            { IsDefaultOrEmpty: true } => [
                new()
                {
                    VertexOffset = 0,
                    IndexOffset = 0,
                    IndexCount = desc.Indices.Data.Length,
                },
            ],
            _ => desc.Submeshes,
        };
        var mesh = new Mesh(screen, meshData, submeshes);
        return Own.New(mesh, static x => SafeCast.As<Mesh>(x).Release());
    }

    public static Own<Mesh> Create<TVertex, TIndex>(
        Screen screen,
        ReadOnlySpanU32<TVertex> vertices,
        ReadOnlySpanU32<TIndex> indices,
        BufferUsages usages = DefaultUsages)
        where TVertex : unmanaged, IVertex
        where TIndex : unmanaged, INumberBase<TIndex>
    {
        return Create(screen, new MeshDescriptor<TVertex, TIndex>
        {
            Vertices = new() { Data = vertices, Usages = usages },
            Indices = new() { Data = indices, Usages = usages },
        });
    }

    public unsafe static Own<Mesh> CreateWithTangent<TVertex, TIndex>(
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
            return Create(screen, new MeshDescriptor<TVertex, TIndex>
            {
                Vertices = new() { Data = vertices, Usages = usages },
                Indices = new() { Data = indices, Usages = usages },
                Tangents = new() { Data = tangents, Usages = usages },
            });
        }
        finally {
            NativeMemory.Free(tp);
        }
    }

    public static Own<Mesh> CreateWithTangent<TVertex, TIndex>(
        Screen screen,
        ReadOnlySpanU32<TVertex> vertices,
        ReadOnlySpanU32<TIndex> indices,
        ReadOnlySpanU32<Vector3> tangents,
        BufferUsages usages = DefaultUsages)
        where TVertex : unmanaged, IVertex
        where TIndex : unmanaged, INumberBase<TIndex>
    {
        return Create(screen, new MeshDescriptor<TVertex, TIndex>
        {
            Vertices = new() { Data = vertices, Usages = usages },
            Indices = new() { Data = indices, Usages = usages },
            Tangents = new() { Data = tangents, Usages = usages },
        });
    }
}
