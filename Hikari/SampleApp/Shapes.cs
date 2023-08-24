#nullable enable
using System;

namespace Hikari;

public static class Shapes
{
    public static Own<Mesh<Vertex>> Cube(Screen screen, bool useTangent)
    {
        // [indices]
        //             0 ------- 3
        //             |         |
        //             |   up    |
        //             |         |
        //             1 ------- 2
        // 4 ------- 7 8 -------11 12-------15 16-------19
        // |         | |         | |         | |         |
        // |  left   | |  front  | |  right  | |  back   |
        // |         | |         | |         | |         |
        // 5 ------- 6 9 -------10 13-------14 17-------18
        //             20-------23
        //             |         |
        //             |  down   |
        //             |         |
        //             21-------22
        //
        // [uv]
        //
        //       0 ------ 1/4 ----- 1/2 ----- 3/4 ------ 1
        //
        //   0   o --> u   + ------- +
        //   |   |         |         |
        //   |   v         |   up    |
        //   |             |         |
        //  1/3  + ------- + ------- + ------- + ------- +
        //   |   |         |         |         |         |
        //   |   |  left   |  front  |  right  |  back   |
        //   |   |         |         |         |         |
        //  2/3  + ------- + ------- + ------- + ------- +
        //   |             |         |
        //   |             |  down   |
        //   |             |         |
        //   1             + ------- +
        //
        //     + ------- +
        //    /   up    /|
        //   + ------- + |
        //   |         | ← right
        //   |  front  | +
        //   |         |/
        //   + ------- +

        const float a = 0.5f;
        const float b0 = 0f;
        const float b1 = 1f / 4f;
        const float b2 = 2f / 4f;
        const float b3 = 3f / 4f;
        const float b4 = 1f;
        const float c0 = 0f;
        const float c1 = 1f / 3f;
        const float c2 = 2f / 3f;
        const float c3 = 1f;

        ReadOnlySpan<Vertex> vertices = stackalloc Vertex[24]
        {
            new Vertex(new(-a, a, -a), new(0, 1, 0), new(b1, c0)),
            new Vertex(new(-a, a, a), new(0, 1, 0), new(b1, c1)),
            new Vertex(new(a, a, a), new(0, 1, 0), new(b2, c1)),
            new Vertex(new(a, a, -a), new(0, 1, 0), new(b2, c0)),
            new Vertex(new(-a, a, -a), new(-1, 0, 0), new(b0, c1)),
            new Vertex(new(-a, -a, -a), new(-1, 0, 0), new(b0, c2)),
            new Vertex(new(-a, -a, a), new(-1, 0, 0), new(b1, c2)),
            new Vertex(new(-a, a, a), new(-1, 0, 0), new(b1, c1)),
            new Vertex(new(-a, a, a), new(0, 0, 1), new(b1, c1)),
            new Vertex(new(-a, -a, a), new(0, 0, 1), new(b1, c2)),
            new Vertex(new(a, -a, a), new(0, 0, 1), new(b2, c2)),
            new Vertex(new(a, a, a), new(0, 0, 1), new(b2, c1)),
            new Vertex(new(a, a, a), new(1, 0, 0), new(b2, c1)),
            new Vertex(new(a, -a, a), new(1, 0, 0), new(b2, c2)),
            new Vertex(new(a, -a, -a), new(1, 0, 0), new(b3, c2)),
            new Vertex(new(a, a, -a), new(1, 0, 0), new(b3, c1)),
            new Vertex(new(a, a, -a), new(0, 0, -1), new(b3, c1)),
            new Vertex(new(a, -a, -a), new(0, 0, -1), new(b3, c2)),
            new Vertex(new(-a, -a, -a), new(0, 0, -1), new(b4, c2)),
            new Vertex(new(-a, a, -a), new(0, 0, -1), new(b4, c1)),
            new Vertex(new(-a, -a, a), new(0, -1, 0), new(b1, c2)),
            new Vertex(new(-a, -a, -a), new(0, -1, 0), new(b1, c3)),
            new Vertex(new(a, -a, -a), new(0, -1, 0), new(b2, c3)),
            new Vertex(new(a, -a, a), new(0, -1, 0), new(b2, c2)),
        };

#pragma warning disable IDE0055 // auto code formatting
        ReadOnlySpan<ushort> indices = stackalloc ushort[36]
        {
            0, 1, 2, 2, 3, 0,           // up
            4, 5, 6, 6, 7, 4,           // left
            8, 9, 10, 10, 11, 8,        // front
            12, 13, 14, 14, 15, 12,     // right
            16, 17, 18, 18, 19, 16,     // back
            20, 21, 22, 22, 23, 20,     // down
        };
#pragma warning restore IDE0055 // auto code formatting
        return useTangent ?
            Mesh.CreateWithTangent(screen, vertices, indices) :
            Mesh.Create(screen, vertices, indices);
    }

    public static Own<Mesh<Vertex>> Plane(Screen screen, bool useTangent)
    {
        const float A = 0.5f;
        ReadOnlySpan<Vertex> vertices = stackalloc Vertex[4]
        {
            new Vertex(new(-A, A, 0.0f), new(0, 0, 1), new(0f, 1f)),
            new Vertex(new(-A, -A, 0.0f), new(0, 0, 1), new(0f, 0f)),
            new Vertex(new(A, -A, 0.0f), new(0, 0, 1), new(1f, 0f)),
            new Vertex(new(A, A, 0.0f), new(0, 0, 1), new(1f, 1f)),
        };
        ReadOnlySpan<ushort> indices = stackalloc ushort[] { 0, 1, 2, 2, 3, 0 };
        if(useTangent) {
            return Mesh.CreateWithTangent(screen, vertices, indices);
        }
        else {
            return Mesh.Create(screen, vertices, indices);
        }
    }

    public static Own<Mesh<VertexPosNormal>> RegularIcosahedron(Screen screen)
    {
        const float a = 1f;
        const float s = 1.618033988f * a;
#pragma warning disable IDE0055 // auto code formatting
        ReadOnlySpan<VertexPosNormal> vertices = stackalloc VertexPosNormal[12]
        {
            new VertexPosNormal(new(a, s, 0), new()),     // 0
            new VertexPosNormal(new(-a, s, 0), new()),    // 1
            new VertexPosNormal(new(-a, -s, 0), new()),   // 2
            new VertexPosNormal(new(a, -s, 0), new()),    // 3
            new VertexPosNormal(new(0, a, s), new()),     // 4
            new VertexPosNormal(new(0, -a, s), new()),    // 5
            new VertexPosNormal(new(0, -a, -s), new()),   // 6
            new VertexPosNormal(new(0, a, -s), new()),    // 7
            new VertexPosNormal(new(s, 0, a), new()),     // 8
            new VertexPosNormal(new(s, 0, -a), new()),    // 9
            new VertexPosNormal(new(-s, 0, -a), new()),   // 10
            new VertexPosNormal(new(-s, 0, a), new()),    // 11
        };
        ReadOnlySpan<ushort> indices = stackalloc ushort[60]
        {
            4, 8, 0,
            4, 5, 8,
            4, 11, 5,
            4, 1, 11,
            4, 0, 1,
            7, 1, 0,
            7, 10, 1,
            7, 6, 10,
            7, 9, 6,
            7, 0, 9,
            3, 8, 5,
            3, 9, 8,
            3, 6, 9,
            3, 2, 6,
            3, 5, 2,
            0, 8, 9,
            1, 10, 11,
            10, 6, 2,
            10, 2, 11,
            2, 5, 11,
        };
#pragma warning restore IDE0055 // auto code formatting

        return Mesh.Create(screen, vertices, indices);
    }
}
