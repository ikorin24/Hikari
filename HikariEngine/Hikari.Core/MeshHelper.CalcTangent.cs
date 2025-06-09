#nullable enable
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Hikari;

partial class MeshHelper
{
    public unsafe static void CalcTangent<TVertex, TIndex>(
        ReadOnlySpanU32<TVertex> vertices,
        ReadOnlySpanU32<TIndex> indices,
        SpanU32<Vector3> tangentsUninit,
        bool skipTangentsClear = false)
        where TVertex : unmanaged, IVertex, IVertexUV
        where TIndex : unmanaged, INumberBase<TIndex>
    {
        fixed(TVertex* vp = vertices)
        fixed(TIndex* ip = indices)
        fixed(Vector3* tp = tangentsUninit) {
            CalcTangent(vp, vertices.Length, ip, indices.Length, tp, tangentsUninit.Length, TVertex.UVOffset, skipTangentsClear);
        }
    }

    public unsafe static void CalcTangent<TVertex, TIndex>(
        ReadOnlySpanU32<TVertex> vertices,
        ReadOnlySpanU32<TIndex> indices,
        SpanU32<Vector3> tangentsUninit,
        uint uvOffset,
        bool skipTangentsClear = false)
        where TVertex : unmanaged, IVertex
        where TIndex : unmanaged, INumberBase<TIndex>
    {
        fixed(TVertex* vp = vertices)
        fixed(TIndex* ip = indices)
        fixed(Vector3* tp = tangentsUninit) {
            CalcTangent(vp, vertices.Length, ip, indices.Length, tp, tangentsUninit.Length, uvOffset, skipTangentsClear);
        }
    }

    public unsafe static void CalcTangent<TVertex, TIndex>(
        TVertex* vertices, u32 verticesLen,
        TIndex* indices, u32 indicesLen,
        Vector3* tangents, u32 tangentsLen,
        uint uvOffset,
        bool skipTangentsClear = false)
        where TVertex : unmanaged, IVertex
        where TIndex : unmanaged, INumberBase<TIndex>
    {
        if(verticesLen != tangentsLen) {
            throw new ArgumentException(nameof(tangentsLen));
        }

        IndexTriangle<TIndex>* triangles = (IndexTriangle<TIndex>*)indices;
        u32 trianglesLen = indicesLen / 3;

        if(skipTangentsClear == false) {
            // Clear tangentsUninit[0] to tangentsUninit[tangentsLen - 1]
            if(tangentsLen <= int.MaxValue) {
                new Span<Vector3>(tangents, (int)tangentsLen).Clear();
            }
            else {
                new Span<Vector3>(tangents, int.MaxValue).Clear();
                tangents[int.MaxValue] = default;
                const uint O = (u32)int.MaxValue + 1;
                new Span<Vector3>(tangents + O, (int)(tangentsLen - O)).Clear();
            }
        }

        var counter = (u32*)NativeMemory.AllocZeroed(sizeof(u32), tangentsLen);
        try {
            for(u32 i = 0; i < trianglesLen; i++) {
                var (i0, i1, i2) = triangles[i].ToUsize();
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
            for(u32 i = 0; i < tangentsLen; i++) {
                tangents[i] /= (float)counter[i];
            }
        }
        finally {
            NativeMemory.Free(counter);
        }
    }

    private readonly record struct IndexTriangle<TIndex> where TIndex : unmanaged, INumberBase<TIndex>
    {
#pragma warning disable 0649
        public readonly TIndex I0;
        public readonly TIndex I1;
        public readonly TIndex I2;
#pragma warning restore 0649

        public (usize I0, usize I1, usize i2) ToUsize()
        {
            return (usize.CreateTruncating(I0), usize.CreateTruncating(I1), usize.CreateTruncating(I2));
        }
    }
}
