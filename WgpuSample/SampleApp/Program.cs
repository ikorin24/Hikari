#nullable enable
using Elffy.Effective;
using Elffy.Imaging;
using System;
using System.Diagnostics;
using System.IO;

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
        var albedo = LoadTexture(screen, "pic.png", TextureFormat.Rgba8UnormSrgb, true);
        var mr = LoadTexture(screen, "pic.png", TextureFormat.Rgba8Unorm, true);
        var normal = LoadTexture(screen, "pic.png", TextureFormat.Rgba8Unorm, true);
        var mesh = SampleData.SampleMesh(screen);

        var model = new PbrModel(layer, mesh, sampler, albedo, mr, normal);
        var camera = screen.Camera;
        camera.Position = new Vector3(0, 0, 3);
        camera.LookAt(Vector3.Zero);
    }

    private static Own<Texture> LoadTexture(Screen screen, string filepath, TextureFormat format, bool useMipmap)
    {
        using var image = LoadImage(filepath);
        var mipLevelCount = useMipmap ? uint.Log2(uint.Min((uint)image.Width, (uint)image.Height)) : 1;
        var texture = Texture.Create(screen, new TextureDescriptor
        {
            Size = new Vector3u((uint)image.Width, (uint)image.Height, 1),
            MipLevelCount = mipLevelCount,
            SampleCount = 1,
            Dimension = TextureDimension.D2,
            Format = format,
            Usage = TextureUsages.TextureBinding | TextureUsages.CopyDst,
        });
        var tex = texture.AsValue();
        tex.Write<ColorByte>(0, image.GetPixels());
        for(uint i = 1; i < tex.MipLevelCount; i++) {
            var w = (image.Width >> (int)i);
            var h = (image.Height >> (int)i);
            using var curuent = image.Resized(new Vector2i(w, h));
            tex.Write<ColorByte>(i, curuent.GetPixels());
        }
        return texture;

        static Image LoadImage(string filepath)
        {
            using var stream = File.OpenRead(filepath);
            return Image.FromStream(stream, Path.GetExtension(filepath));
        }
    }
}
