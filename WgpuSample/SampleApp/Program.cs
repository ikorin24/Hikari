#nullable enable
using Elffy.Effective;
using Elffy.Imaging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Elffy;

internal class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Environment.SetEnvironmentVariable("RUST_BACKTRACE", "1");
        var screenConfig = new ScreenConfig
        {
            Backend = GraphicsBackend.Dx12,
            Width = 1280,
            Height = 720,
            Style = WindowStyle.Default,
        };
        Engine.Run(screenConfig, OnInitialized);
    }

    private static void OnInitialized(Screen screen)
    {
        screen.Title = "sample";
        var layer = new PbrLayer(screen, 0);
        var deferredProcess = new DeferredProcess(layer, 1);
        var sampler = Sampler.Create(screen, new()
        {
            AddressModeU = AddressMode.ClampToEdge,
            AddressModeV = AddressMode.ClampToEdge,
            AddressModeW = AddressMode.ClampToEdge,
            MagFilter = FilterMode.Linear,
            MinFilter = FilterMode.Linear,
            MipmapFilter = FilterMode.Linear,
        });

        var albedo = LoadTexture(screen, "resources/ground_0036_color_1k.jpg", true);
        var mr = LoadTexture(screen, "pic.png", false);
        var normal = LoadTexture(screen, "resources/ground_0036_normal_opengl_1k.png", false);
        //var mesh = SampleData.SampleMesh(screen);
        var mesh = Shapes.Cube(screen);

        var model = new PbrModel(layer, mesh, sampler, albedo, mr, normal);
        var camera = screen.Camera;
        camera.Position = new Vector3(2, 6, 9);
        camera.LookAt(Vector3.Zero);
    }

    private static Own<Texture> LoadTexture(Screen screen, string filepath, bool isSrgb)
    {
        var format = isSrgb ? TextureFormat.Rgba8UnormSrgb : TextureFormat.Rgba8Unorm;
        using var image = LoadImage(filepath);
        return Texture.CreateWithAutoMipmap(screen, image, format, TextureUsages.TextureBinding);

        static Image LoadImage(string filepath)
        {
            using var stream = File.OpenRead(filepath);
            return Image.FromStream(stream, Path.GetExtension(filepath));
        }
    }
}

public static class Shapes
{
    public static Own<Mesh<Vertex>> Cube(Screen screen)
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
        // OpenGL coordinate of uv is left-bottom based,
        // but many popular format of images (e.g. png) are left-top based.
        // So, I use left-top as uv coordinate.
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

        // up, left, front, right, back, down
        ReadOnlySpan<ushort> indices = stackalloc ushort[36] { 0, 1, 2, 2, 3, 0, 4, 5, 6, 6, 7, 4, 8, 9, 10, 10, 11, 8, 12, 13, 14, 14, 15, 12, 16, 17, 18, 18, 19, 16, 20, 21, 22, 22, 23, 20 };

        return Mesh.CreateWithTangent(screen, vertices, indices);
    }
}
