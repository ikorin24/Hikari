#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using U = System.Runtime.CompilerServices.Unsafe;
using UEx = Hikari.UnsafeEx;

namespace Hikari;

public static unsafe class VertexHelper
{
    public static void CalcTangents<TVertex>(Span<TVertex> vertices)
        where TVertex : unmanaged, IVertex, IVertexPosition, IVertexUV, IVertexTangent
    {
        CalcTangentsPrivate(ref vertices.GetReference(), (nuint)vertices.Length);
    }

    public static void CalcTangents<TVertex>(TVertex* vertices, nuint vLength)
        where TVertex : unmanaged, IVertex, IVertexPosition, IVertexUV, IVertexTangent
    {
        CalcTangentsPrivate(ref *vertices, vLength);
    }

    public static void CalcTangentsSeparated<TVertex>(ReadOnlySpan<TVertex> vertices, Span<Vector3> tangents)
        where TVertex : unmanaged, IVertex, IVertexPosition, IVertexUV
    {
        CalcTangentsSeparetedPrivate(in vertices.GetReference(), (nuint)vertices.Length, ref tangents.GetReference(), (nuint)tangents.Length);
    }

    public static void CalcTangentsSeparated<TVertex>(TVertex* vertices, nuint vLength, Vector3* tangents, nuint tanLength)
        where TVertex : unmanaged, IVertex, IVertexPosition, IVertexUV
    {
        CalcTangentsSeparetedPrivate(in *vertices, (nuint)vLength, ref *tangents, (nuint)tanLength);
    }

    public static void CalcTangentsIndexed<TVertex>(Span<TVertex> vertices, ReadOnlySpan<uint> indices)
        where TVertex : unmanaged, IVertex, IVertexPosition, IVertexUV, IVertexTangent
    {
        CalcTangentsIndexedPrivate(ref vertices.GetReference(), (nuint)vertices.Length, in indices.GetReference(), (nuint)indices.Length);
    }

    public static void CalcTangentsIndexed<TVertex>(TVertex* vertices, nuint vLength, uint* indices, nuint iLength)
        where TVertex : unmanaged, IVertex, IVertexPosition, IVertexUV, IVertexTangent
    {
        CalcTangentsIndexedPrivate(ref *vertices, vLength, in *indices, iLength);
    }

    public static void CalcTangentsSeparatedIndexed<TVertex>(ReadOnlySpan<TVertex> vertices, Span<Vector3> tangents, ReadOnlySpan<uint> indices, bool isTangentsBufferZeroCleared)
        where TVertex : unmanaged, IVertex, IVertexPosition, IVertexUV
    {
        CalcTangentsSeparatedIndexedPrivate(
            in vertices.GetReference(), (nuint)vertices.Length,
            ref tangents.GetReference(), (nuint)tangents.Length,
            in indices.GetReference(), (nuint)indices.Length,
            isTangentsBufferZeroCleared);
    }

    public static void CalcTangentsSeparatedIndexed<TVertex>(
        TVertex* vertices, nuint vLength,
        Vector3* tangents, nuint tanLength,
        uint* indices, nuint iLength,
        bool isTangentsBufferZeroCleared)
        where TVertex : unmanaged, IVertex, IVertexPosition, IVertexUV
    {
        CalcTangentsSeparatedIndexedPrivate(
            in *vertices, vLength,
            ref *tangents, tanLength,
            in *indices, iLength,
            isTangentsBufferZeroCleared);
    }

    private static void CalcTangentsPrivate<TVertex>(ref TVertex vertices, nuint vLength)
        where TVertex : unmanaged, IVertex, IVertexPosition, IVertexUV, IVertexTangent
    {
        // non indexed vertices

        for(nuint i = 0; i < vLength / 3; i++) {
            var i0 = i * 3;
            var i1 = i * 3 + 1;
            var i2 = i * 3 + 2;
            var tangent = CalcTangent(
                VertexAccessor.Position(UEx.Add(vertices, i0)),
                VertexAccessor.Position(UEx.Add(vertices, i1)),
                VertexAccessor.Position(UEx.Add(vertices, i2)),
                VertexAccessor.UV(UEx.Add(vertices, i0)),
                VertexAccessor.UV(UEx.Add(vertices, i1)),
                VertexAccessor.UV(UEx.Add(vertices, i2))).Normalized();

            VertexAccessor.RefTangent(ref U.Add(ref vertices, i0)) = tangent;
            VertexAccessor.RefTangent(ref U.Add(ref vertices, i1)) = tangent;
            VertexAccessor.RefTangent(ref U.Add(ref vertices, i2)) = tangent;
        }
    }

    private static void CalcTangentsSeparetedPrivate<TVertex>(
        in TVertex vertices, nuint vLength,
        ref Vector3 tangents, nuint tanLength)
        where TVertex : unmanaged, IVertex, IVertexPosition, IVertexUV
    {
        // non indexed vertices

        if(tanLength < vLength) {
            throw new ArgumentOutOfRangeException(nameof(tangents), "tangents.Length must be greater than or equal to vertices.Length.");
        }

        for(nuint i = 0; i < vLength / 3; i++) {
            var i0 = i * 3;
            var i1 = i * 3 + 1;
            var i2 = i * 3 + 2;
            if(i0 >= vLength) { ThrowIndexOutOfRange(nameof(vertices), i0, vLength); }
            if(i1 >= vLength) { ThrowIndexOutOfRange(nameof(vertices), i1, vLength); }
            if(i2 >= vLength) { ThrowIndexOutOfRange(nameof(vertices), i2, vLength); }
            var tangent = CalcTangent(
                VertexAccessor.Position(UEx.Add(vertices, i0)),
                VertexAccessor.Position(UEx.Add(vertices, i1)),
                VertexAccessor.Position(UEx.Add(vertices, i2)),
                VertexAccessor.UV(UEx.Add(vertices, i0)),
                VertexAccessor.UV(UEx.Add(vertices, i1)),
                VertexAccessor.UV(UEx.Add(vertices, i2))).Normalized();

            U.Add(ref tangents, i0) = tangent;
            U.Add(ref tangents, i1) = tangent;
            U.Add(ref tangents, i2) = tangent;
        }
    }

    private static void CalcTangentsIndexedPrivate<TVertex>(ref TVertex vertices, nuint vLength, in uint indices, nuint iLength)
        where TVertex : unmanaged, IVertex, IVertexPosition, IVertexUV, IVertexTangent
    {
        // indexed vertices

        for(nuint i = 0; i < vLength; i++) {
            VertexAccessor.RefTangent(ref U.Add(ref vertices, i)) = default;
        }

        for(nuint i = 0; i < iLength / 3; i++) {
            var i0 = UEx.Add(indices, i * 3);
            var i1 = UEx.Add(indices, i * 3 + 1);
            var i2 = UEx.Add(indices, i * 3 + 2);
            if(i0 >= vLength) { ThrowIndexOutOfRange(nameof(vertices), i0, vLength); }
            if(i1 >= vLength) { ThrowIndexOutOfRange(nameof(vertices), i1, vLength); }
            if(i2 >= vLength) { ThrowIndexOutOfRange(nameof(vertices), i2, vLength); }
            var tangent = CalcTangent(
                VertexAccessor.Position(UEx.Add(vertices, i0)),
                VertexAccessor.Position(UEx.Add(vertices, i1)),
                VertexAccessor.Position(UEx.Add(vertices, i2)),
                VertexAccessor.UV(UEx.Add(vertices, i0)),
                VertexAccessor.UV(UEx.Add(vertices, i1)),
                VertexAccessor.UV(UEx.Add(vertices, i2))
            );
            VertexAccessor.RefTangent(ref U.Add(ref vertices, i0)) += tangent;
            VertexAccessor.RefTangent(ref U.Add(ref vertices, i1)) += tangent;
            VertexAccessor.RefTangent(ref U.Add(ref vertices, i2)) += tangent;
        }
        for(nuint i = 0; i < vLength; i++) {
            VertexAccessor.RefTangent(ref U.Add(ref vertices, i)).Normalize();
        }
    }

    private static void CalcTangentsSeparatedIndexedPrivate<TVertex>(
        in TVertex vertices, nuint vLength,
        ref Vector3 tangents, nuint tanLength,
        in uint indices, nuint iLength,
        bool isTangentsBufferZeroCleared)
        where TVertex : unmanaged, IVertex, IVertexPosition, IVertexUV
    {
        // indexed vertices
        if(tanLength < vLength) {
            throw new ArgumentOutOfRangeException(nameof(tangents), "tangents.Length must be greater than or equal to vertices.Length.");
        }

        if(isTangentsBufferZeroCleared == false) {
            if(tanLength <= int.MaxValue) {
                MemoryMarshal.CreateSpan(ref tangents, (int)tanLength).Clear();
            }
            else {
                for(nuint i = 0; i < tanLength; i++) {
                    U.Add(ref tangents, i) = default;
                }
            }
        }

        for(nuint i = 0; i < iLength / 3; i++) {
            var i0 = UEx.Add(indices, i * 3);
            var i1 = UEx.Add(indices, i * 3 + 1);
            var i2 = UEx.Add(indices, i * 3 + 2);
            if(i0 >= vLength) { ThrowIndexOutOfRange(nameof(vertices), i0, vLength); }
            if(i1 >= vLength) { ThrowIndexOutOfRange(nameof(vertices), i1, vLength); }
            if(i2 >= vLength) { ThrowIndexOutOfRange(nameof(vertices), i2, vLength); }
            var tangent = CalcTangent(
                VertexAccessor.Position(UEx.Add(vertices, i0)),
                VertexAccessor.Position(UEx.Add(vertices, i1)),
                VertexAccessor.Position(UEx.Add(vertices, i2)),
                VertexAccessor.UV(UEx.Add(vertices, i0)),
                VertexAccessor.UV(UEx.Add(vertices, i1)),
                VertexAccessor.UV(UEx.Add(vertices, i2))
            );

            U.Add(ref tangents, i0) += tangent;
            U.Add(ref tangents, i1) += tangent;
            U.Add(ref tangents, i2) += tangent;
        }
        for(nuint i = 0; i < vLength; i++) {
            U.Add(ref tangents, i).Normalize();
        }
    }

    private static Vector3 CalcTangent(in Vector3 pos0, in Vector3 pos1, in Vector3 pos2, in Vector2 uv0, in Vector2 uv1, in Vector2 uv2)
    {
        var deltaUV1 = uv1 - uv0;
        var deltaUV2 = uv2 - uv0;
        var deltaPos1 = pos1 - pos0;
        var deltaPos2 = pos2 - pos0;
        var d = 1f / (deltaUV1.X * deltaUV2.Y - deltaUV1.Y * deltaUV2.X);
        var tangent = d * (deltaUV2.Y * deltaPos1 - deltaUV1.Y * deltaPos2);
#if DEBUG
        var bitangent = d * (deltaUV1.X * deltaPos2 - deltaUV2.X * deltaPos1);
#endif
        return tangent;
    }

    [DoesNotReturn]
    [DebuggerHidden]
    private static void ThrowIndexOutOfRange(string name, nuint index, nuint len) =>
        throw new IndexOutOfRangeException($"Index was outside the bounds of the array. (index: {index}, {name}.Length: {len})");
}
