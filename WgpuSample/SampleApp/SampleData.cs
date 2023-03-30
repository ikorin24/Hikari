#nullable enable
using System;
using System.IO;
using Elffy.Effective;
using Elffy.Imaging;

namespace Elffy;

public static class SampleData
{
    public static Own<Mesh<VertexSlim>> SampleMesh(Screen screen)
    {
        const float A = 0.3f;
        ReadOnlySpan<VertexSlim> vertices = stackalloc VertexSlim[4]
        {
            new VertexSlim(new(-A, A, 0.0f), new(0f, 0f)),
            new VertexSlim(new(-A, -A, 0.0f), new(0f, 1f)),
            new VertexSlim(new(A, -A, 0.0f), new(1f, 1f)),
            new VertexSlim(new(A, A, 0.0f), new(1f, 0f)),
        };
        ReadOnlySpan<ushort> indices = stackalloc ushort[] { 0, 1, 2, 2, 3, 0 };
        return Mesh<VertexSlim>.Create(screen, vertices, indices);
    }

    public static Own<Texture> SampleTexture(Screen screen)
    {
        var filepath = "pic.png";
        using var stream = File.OpenRead(filepath);
        using var image = Image.FromStream(stream, Path.GetExtension(filepath));

        var texture = Texture.Create(screen, new TextureDescriptor
        {
            Size = new Vector3u((uint)image.Width, (uint)image.Height, 1),
            MipLevelCount = 1,
            SampleCount = 1,
            Dimension = TextureDimension.D2,
            Format = TextureFormat.Rgba8UnormSrgb,
            Usage = TextureUsages.TextureBinding | TextureUsages.CopyDst,
        });
        texture.AsValue().Write(0, image.GetPixels().AsReadOnly());
        return texture;
    }
}
