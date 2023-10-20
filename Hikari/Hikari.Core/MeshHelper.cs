#nullable enable
using Hikari.Collections;
using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Hikari;

public static partial class MeshHelper
{
    public unsafe static void CalcTangentsU32<TVertex>(
        TVertex* vertices, u32 verticesLen,
        u32* indices, u32 indicesLen,
        Vector3* tangents)
        where TVertex : unmanaged, IVertex, IVertexUV
    {
        CalcTangentsU32(vertices, verticesLen, indices, indicesLen, tangents, TVertex.UVOffset);
    }

    public unsafe static void CalcTangentsU16<TVertex>(
        TVertex* vertices, u32 verticesLen,
        u16* indices, u32 indicesLen,
        Vector3* tangentsUninit)
        where TVertex : unmanaged, IVertex, IVertexUV
    {
        CalcTangentsU16(vertices, verticesLen, indices, indicesLen, tangentsUninit, TVertex.UVOffset);
    }

    public unsafe static void CalcTangentsU32<TVertex>(
        TVertex* vertices, u32 verticesLen,
        u32* indices, u32 indicesLen,
        Vector3* tangentsUninit, uint uvOffset)
        where TVertex : unmanaged, IVertex
    {
        Vector3u* triangles = (Vector3u*)indices;
        u32 trianglesLen = indicesLen / 3;

        // Clear tangentsUninit[0] to tangentsUninit[verticesLen - 1]
        if(verticesLen <= int.MaxValue) {
            new Span<Vector3>(tangentsUninit, (int)verticesLen).Clear();
        }
        else {
            new Span<Vector3>(tangentsUninit, int.MaxValue).Clear();
            tangentsUninit[int.MaxValue] = default;
            const uint O = (u32)int.MaxValue + 1;
            new Span<Vector3>(tangentsUninit + O, (int)(verticesLen - O)).Clear();
        }
        Vector3* tangents = tangentsUninit;

        var counter = (u32*)NativeMemory.AllocZeroed(sizeof(u32), verticesLen);
        try {
            for(u32 i = 0; i < trianglesLen; i++) {
                var (i0, i1, i2) = triangles[i];
                ref readonly var p0 = ref VertexAccessor.Position(vertices[i0]);
                ref readonly var uv0 = ref VertexAccessor.GetField<TVertex, Vector2>(vertices[i0], uvOffset);
                ref readonly var p1 = ref VertexAccessor.Position(vertices[i1]);
                ref readonly var uv1 = ref VertexAccessor.GetField<TVertex, Vector2>(vertices[i1], uvOffset);
                ref readonly var p2 = ref VertexAccessor.Position(vertices[i2]);
                ref readonly var uv2 = ref VertexAccessor.GetField<TVertex, Vector2>(vertices[i2], uvOffset);

                var deltaUV1 = uv1 - uv0;
                var deltaUV2 = uv2 - uv0;
                var deltaPos1 = p1 - p0;
                var deltaPos2 = p2 - p0;
                var d = 1f / (deltaUV1.X * deltaUV2.Y - deltaUV1.Y * deltaUV2.X);
                var tangent = d * (deltaUV2.Y * deltaPos1 - deltaUV1.Y * deltaPos2);
                tangents[i0] += tangent;
                tangents[i1] += tangent;
                tangents[i2] += tangent;
                counter[i0]++;
                counter[i1]++;
                counter[i2]++;
#if DEBUG
                var bitangent = d * (deltaUV1.X * deltaPos2 - deltaUV2.X * deltaPos1);
#endif
            }
            for(u32 i = 0; i < verticesLen; i++) {
                tangents[i] /= (float)counter[i];
            }
        }
        finally {
            NativeMemory.Free(counter);
        }
    }

    public unsafe static void CalcTangentsU16<TVertex>(
        TVertex* vertices, u32 verticesLen,
        u16* indices, u32 indicesLen,
        Vector3* tangentsUninit,
        uint uvOffset)
        where TVertex : unmanaged, IVertex
    {
        U16x3* triangles = (U16x3*)indices;
        u32 trianglesLen = indicesLen / 3;

        // Clear tangentsUninit[0] to tangentsUninit[verticesLen - 1]
        if(verticesLen <= int.MaxValue) {
            new Span<Vector3>(tangentsUninit, (int)verticesLen).Clear();
        }
        else {
            new Span<Vector3>(tangentsUninit, int.MaxValue).Clear();
            tangentsUninit[int.MaxValue] = default;
            const uint O = (u32)int.MaxValue + 1;
            new Span<Vector3>(tangentsUninit + O, (int)(verticesLen - O)).Clear();
        }
        Vector3* tangents = tangentsUninit;

        var counter = (u32*)NativeMemory.AllocZeroed(sizeof(u32), verticesLen);
        try {
            for(u32 i = 0; i < trianglesLen; i++) {
                var (i0, i1, i2) = triangles[i];
                ref readonly var p0 = ref VertexAccessor.Position(vertices[i0]);
                ref readonly var uv0 = ref VertexAccessor.GetField<TVertex, Vector2>(vertices[i0], uvOffset);
                ref readonly var p1 = ref VertexAccessor.Position(vertices[i1]);
                ref readonly var uv1 = ref VertexAccessor.GetField<TVertex, Vector2>(vertices[i1], uvOffset);
                ref readonly var p2 = ref VertexAccessor.Position(vertices[i2]);
                ref readonly var uv2 = ref VertexAccessor.GetField<TVertex, Vector2>(vertices[i2], uvOffset);

                var deltaUV1 = uv1 - uv0;
                var deltaUV2 = uv2 - uv0;
                var deltaPos1 = p1 - p0;
                var deltaPos2 = p2 - p0;
                var d = 1f / (deltaUV1.X * deltaUV2.Y - deltaUV1.Y * deltaUV2.X);
                var tangent = d * (deltaUV2.Y * deltaPos1 - deltaUV1.Y * deltaPos2);
                tangents[i0] += tangent;
                tangents[i1] += tangent;
                tangents[i2] += tangent;
                counter[i0]++;
                counter[i1]++;
                counter[i2]++;
#if DEBUG
                var bitangent = d * (deltaUV1.X * deltaPos2 - deltaUV2.X * deltaPos1);
#endif
            }
            for(u32 i = 0; i < verticesLen; i++) {
                tangents[i] /= (float)counter[i];
            }
        }
        finally {
            NativeMemory.Free(counter);
        }
    }

    private record struct U16x3(u16 X, u16 Y, u16 Z);
}

partial class MeshHelper
{
    public static void CalcNormal<TBufferWriter>(
        ReadOnlySpan<Vector3> positions,
        ReadOnlySpan<int> indices,
        TBufferWriter normalsBuffer) where TBufferWriter : IBufferWriter<Vector3>
    {
        var normals = normalsBuffer.GetSpan(positions.Length);
        CalcNormal(positions, indices, normals);
        normalsBuffer.Advance(normals.Length);
        return;
    }

    public static void CalcNormal(
        ReadOnlySpan<Vector3> positions,
        ReadOnlySpan<int> indices,
        Span<Vector3> normals)
    {
        if(indices.Length % 3 != 0) {
            ThrowArgumentIndicesLengthInvalid();
        }

        // [NOTE]
        // Sharp edge is not supported.

        normals.Clear();

        using var countsBuf = new ValueTypeRentMemory<int>(positions.Length, true);
        var counts = countsBuf.AsSpan();

        var faces = indices.MarshalCast<int, Face>();
        foreach(var f in faces) {
            var n = Vector3.Cross(positions[f.I1] - positions[f.I0], positions[f.I2] - positions[f.I0]).Normalized();
            normals[f.I0] += n;
            normals[f.I1] += n;
            normals[f.I2] += n;
            counts[f.I0] += 1;
            counts[f.I1] += 1;
            counts[f.I2] += 1;
        }
        for(int i = 0; i < positions.Length; i++) {
            normals[i] /= counts[i];
        }
    }

    public static void CalcNormal<TVertex>(Span<TVertex> vertices, ReadOnlySpan<int> indices)  // TODO: something wrong
        where TVertex : unmanaged, IVertex, IVertexNormal
    {
        if(indices.Length % 3 != 0) {
            ThrowArgumentIndicesLengthInvalid();
        }

        for(int i = 0; i < vertices.Length; i++) {
            VertexAccessor.RefNormal(ref vertices[i]) = default;
        }

        using var counts = new ValueTypeRentMemory<int>(vertices.Length, true);
        var faces = indices.MarshalCast<int, Face>();
        foreach(var f in faces) {
            var n = Vector3.Cross(
                VertexAccessor.RefPosition(ref vertices[f.I1]) - VertexAccessor.RefPosition(ref vertices[f.I0]),
                VertexAccessor.RefPosition(ref vertices[f.I2]) - VertexAccessor.RefPosition(ref vertices[f.I0])).Normalized();

            VertexAccessor.RefNormal(ref vertices[f.I0]) += n;
            VertexAccessor.RefNormal(ref vertices[f.I1]) += n;
            VertexAccessor.RefNormal(ref vertices[f.I2]) += n;
            counts[f.I0] += 1;
            counts[f.I1] += 1;
            counts[f.I2] += 1;
        }

        for(int i = 0; i < vertices.Length; i++) {
            VertexAccessor.RefNormal(ref vertices[i]) /= counts[i];
        }
    }

    public static (Vertex[] vertices, int[] indices) CreateInterleavedVertices(
        ReadOnlySpan<Vector3> positions, ReadOnlySpan<int> positionIndices,
        ReadOnlySpan<Vector3> normals, ReadOnlySpan<int> normalIndices,
        ReadOnlySpan<Vector2> uvs, ReadOnlySpan<int> uvIndices)
    {
        using var vertices = new UnsafeRawArray<Vertex>(positionIndices.Length, false);
        using var indices = new UnsafeRawArray<int>(positionIndices.Length, false);
        var (vLen, iLen) = CreateInterleavedVertices(positions, positionIndices, normals, normalIndices, uvs, uvIndices, vertices.AsSpan(), indices.AsSpan());
        return (vertices.AsSpan(0, vLen).ToArray(), indices.AsSpan(0, iLen).ToArray());
    }

    public static void CreateInterleavedVertices(
        ReadOnlySpan<Vector3> positions, ReadOnlySpan<int> positionIndices,
        ReadOnlySpan<Vector3> normals, ReadOnlySpan<int> normalIndices,
        ReadOnlySpan<Vector2> uvs, ReadOnlySpan<int> uvIndices,
        IBufferWriter<Vertex> verticesWriter, IBufferWriter<int> indicesWriter)
    {
        var (vLen, iLen) = CreateInterleavedVertices(
            positions, positionIndices,
            normals, normalIndices,
            uvs, uvIndices,
            verticesWriter.GetSpan(positionIndices.Length), indicesWriter.GetSpan(positionIndices.Length));
        verticesWriter.Advance(vLen);
        indicesWriter.Advance(iLen);
    }

    public static (int VertexCount, int IndexCount) CreateInterleavedVertices(
        ReadOnlySpan<Vector3> positions, ReadOnlySpan<int> positionIndices,
        ReadOnlySpan<Vector3> normals, ReadOnlySpan<int> normalIndices,
        ReadOnlySpan<Vector2> uvs, ReadOnlySpan<int> uvIndices,
        Span<Vertex> vertices, Span<int> indices)
    {
        // vertices.Length and indices.Length may be required to be the same as positionIndices.Length at most.

        var isValid = (positionIndices.Length == normalIndices.Length) && (positionIndices.Length == uvIndices.Length);
        if(isValid == false) {
            throw new ArgumentException();
        }

        var len = positionIndices.Length;
        using var dic = new BufferPooledDictionary<Key_PNT, int>(len);
        var vertexCount = 0;
        var indexCount = 0;

        for(int i = 0; i < len; i++) {
            var key = new Key_PNT(positionIndices[i], normalIndices[i], uvIndices[i]);
            if(dic.TryGetValue(key, out var index)) {
                indices[indexCount++] = index;
            }
            else {
                index = dic.Count;
                indices[indexCount++] = index;
                dic.Add(key, index);
                vertices[vertexCount++] = new Vertex(positions[key.P], normals[key.N], uvs[key.T]);
            }
        }
        return (vertexCount, indexCount);
    }

    private record struct Face(int I0, int I1, int I2);
    private record struct Key_PNT(int P, int N, int T);

    [DoesNotReturn]
    private static void ThrowArgumentIndicesLengthInvalid() => throw new ArgumentException();
}
