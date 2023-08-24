#nullable enable
using System;
using System.Runtime.InteropServices;

namespace Hikari;

internal static class MeshHelper
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
