#nullable enable
using System;
using System.IO;
using Elffy.Imaging;

namespace Elffy;

public static class SampleData
{
    public static Own<Mesh<MyVertex>> SampleMesh(Screen screen)
    {
        const float A = 0.3f;
        ReadOnlySpan<MyVertex> vertices = stackalloc MyVertex[4]
        {
            new MyVertex
            {
                Pos = new(-A, A, 0.0f),
                UV = new(0f, 0f),
            },
            new MyVertex
            {
                Pos = new(-A, -A, 0.0f),
                UV = new(0f, 1f),
            },
            new MyVertex
            {
                Pos = new(A, -A, 0.0f),
                UV = new(1f, 1f),
            },
            new MyVertex
            {
                Pos = new(A, A, 0.0f),
                UV = new(1f, 0f),
            },
        };
        ReadOnlySpan<ushort> indices = stackalloc ushort[] { 0, 1, 2, 2, 3, 0 };
        return Mesh<MyVertex>.Create(screen, vertices, indices);
    }

    public static Image LoadImage(string filepath)
    {
        using var stream = File.OpenRead(filepath);
        return Image.FromStream(stream, Path.GetExtension(filepath));
    }
}
