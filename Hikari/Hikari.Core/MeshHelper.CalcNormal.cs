#nullable enable
using Hikari.Collections;
using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;

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

    private record struct Face(int I0, int I1, int I2);

    [DoesNotReturn]
    private static void ThrowArgumentIndicesLengthInvalid() => throw new ArgumentException();
}
