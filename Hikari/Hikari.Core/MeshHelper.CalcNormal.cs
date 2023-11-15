#nullable enable
using Hikari.Collections;
using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Hikari;

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
            ThrowHelper.ThrowArgument("invalid length of indices");
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

    public static void CalcNormal<TVertex, TIndex>(
        SpanU32<TVertex> vertices,
        ReadOnlySpanU32<TIndex> indices)
        where TVertex : unmanaged, IVertex, IVertexNormal
        where TIndex : unmanaged, INumberBase<TIndex>
    {
        if(indices.Length % 3 != 0) {
            ThrowHelper.ThrowArgument("invalid length of indices");
        }
        for(int i = 0; i < vertices.Length; i++) {
            VertexAccessor.RefNormal(ref vertices[i]) = default;
        }

        if(vertices.Length <= int.MaxValue) {
            using var rentMemory = new ValueTypeRentMemory<u64>((int)vertices.Length, true);
            Calc(vertices, indices, rentMemory.AsSpan());
        }
        else {
            unsafe {
                var ptr = (u64*)NativeMemory.AllocZeroed((usize)sizeof(u64) * vertices.Length);
                try {
                    Calc(vertices, indices, new SpanU32<u64>(ptr, vertices.Length));
                }
                finally {
                    NativeMemory.Free(ptr);
                }
            }
        }

        static void Calc(SpanU32<TVertex> vertices, ReadOnlySpanU32<TIndex> indices, SpanU32<u64> counts)
        {
            var faces = new ReadOnlySpanU32<IndexTriangle<TIndex>>(
                in UnsafeEx.As<TIndex, IndexTriangle<TIndex>>(in indices.GetReference()),
                indices.Length / 3);

            foreach(var f in faces) {
                var (i0, i1, i2) = f.ToUsize();
                var n = Vector3.Cross(
                    VertexAccessor.RefPosition(ref vertices[i1]) - VertexAccessor.RefPosition(ref vertices[i0]),
                    VertexAccessor.RefPosition(ref vertices[i2]) - VertexAccessor.RefPosition(ref vertices[i0])).Normalized();

                VertexAccessor.RefNormal(ref vertices[i0]) += n;
                VertexAccessor.RefNormal(ref vertices[i1]) += n;
                VertexAccessor.RefNormal(ref vertices[i2]) += n;
                counts[i0] += 1;
                counts[i1] += 1;
                counts[i2] += 1;
            }

            for(int i = 0; i < vertices.Length; i++) {
                VertexAccessor.RefNormal(ref vertices[i]) /= counts[i];
            }
        }
    }

    private record struct Face(int I0, int I1, int I2);
}
