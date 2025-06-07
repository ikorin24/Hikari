#nullable enable
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hikari;

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
            vertexBuffer = Buffer.Create(screen, (u8*)v, desc.Vertices.Data.ByteLength, desc.Vertices.Usages | BufferUsages.Vertex);
            indexBuffer = Buffer.Create(screen, (u8*)ind, desc.Indices.Data.ByteLength, desc.Indices.Usages | BufferUsages.Index);
        }
        Own<Buffer> tangentBuffer;
        if(desc.Tangents.Data.IsEmpty) {
            tangentBuffer = Own<Buffer>.None;
        }
        else {
            fixed(void* t = desc.Tangents.Data) {
                tangentBuffer = Buffer.Create(screen, (u8*)t, desc.Tangents.Data.ByteLength, desc.Tangents.Usages | BufferUsages.Vertex);
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
            Fields = TVertex.Fields,
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
