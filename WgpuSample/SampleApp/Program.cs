#nullable enable
using Elffy.Imaging;
using Elffy.Mathematics;
using System;
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

        var albedo = LoadTexture(screen, "resources/ground_0036_color_1k.jpg", true);
        var mr = LoadRoughnessAOTexture(screen, "resources/ground_0036_roughness_1k.jpg", "resources/ground_0036_ao_1k.jpg");
        var normal = LoadTexture(screen, "resources/ground_0036_normal_opengl_1k.png", false);

        var model = new PbrModel(
                layer,
                SampleData.SampleMesh(screen),
                PbrMaterial.Create(
                    layer.Shader,
                    albedo,
                    mr,
                    normal));
        model.Rotation = Quaternion.FromAxisAngle(Vector3.UnitX, -90.ToRadian());
        var cube = new PbrModel(
                layer,
                Shapes.Cube(screen, true),
                PbrMaterial.Create(layer.Shader, albedo.AsValue(), mr.AsValue(), normal.AsValue()));
        cube.Scale = 0.3f;
        cube.Position = new Vector3(0, 0.2f, 0);


        var camera = screen.Camera;
        camera.LookAt(Vector3.Zero, new Vector3(0, 2f, 3) * 0.6f);

        screen.Update.Subscribe(screen =>
        {
            //System.Diagnostics.Debug.WriteLine(screen.FrameNum);
            var a = (screen.FrameNum * 10 / 360f).ToRadian();
            cube.Rotation = Quaternion.FromAxisAngle(Vector3.UnitY, a);
        });
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

    private static Own<Texture> LoadRoughnessAOTexture(Screen screen, string filepath, string aoFilePath)
    {
        var format = TextureFormat.Rgba8Unorm;
        using var image = LoadImage(filepath);
        using var aoImage = LoadImage(aoFilePath);
        var aoPixels = aoImage.GetPixels();

        var pixels = image.GetPixels();
        for(int i = 0; i < pixels.Length; i++) {
            pixels[i] = new ColorByte(0x00, pixels[i].G, aoPixels[i].R, 0x00);
        }
        return Texture.CreateWithAutoMipmap(screen, image, format, TextureUsages.TextureBinding);

        static Image LoadImage(string filepath)
        {
            using var stream = File.OpenRead(filepath);
            return Image.FromStream(stream, Path.GetExtension(filepath));
        }
    }
}
