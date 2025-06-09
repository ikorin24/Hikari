#nullable enable
using Hikari.Collections;
using System;
using System.Buffers;

namespace Hikari;

public static partial class MeshHelper
{
    public static (Vertex[] vertices, int[] indices) CreateInterleaved(
        ReadOnlySpan<Vector3> positions, ReadOnlySpan<int> positionIndices,
        ReadOnlySpan<Vector3> normals, ReadOnlySpan<int> normalIndices,
        ReadOnlySpan<Vector2> uvs, ReadOnlySpan<int> uvIndices)
    {
        using var vertices = new UnsafeRawArray<Vertex>(positionIndices.Length, false);
        using var indices = new UnsafeRawArray<int>(positionIndices.Length, false);
        var (vLen, iLen) = CreateInterleaved(positions, positionIndices, normals, normalIndices, uvs, uvIndices, vertices.AsSpan(), indices.AsSpan());
        return (vertices.AsSpan(0, vLen).ToArray(), indices.AsSpan(0, iLen).ToArray());
    }

    public static void CreateInterleaved(
        ReadOnlySpan<Vector3> positions, ReadOnlySpan<int> positionIndices,
        ReadOnlySpan<Vector3> normals, ReadOnlySpan<int> normalIndices,
        ReadOnlySpan<Vector2> uvs, ReadOnlySpan<int> uvIndices,
        IBufferWriter<Vertex> verticesWriter, IBufferWriter<int> indicesWriter)
    {
        var (vLen, iLen) = CreateInterleaved(
            positions, positionIndices,
            normals, normalIndices,
            uvs, uvIndices,
            verticesWriter.GetSpan(positionIndices.Length), indicesWriter.GetSpan(positionIndices.Length));
        verticesWriter.Advance(vLen);
        indicesWriter.Advance(iLen);
    }

    public static (int VertexCount, int IndexCount) CreateInterleaved(
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
}

file record struct Key_PNT(int P, int N, int T);
