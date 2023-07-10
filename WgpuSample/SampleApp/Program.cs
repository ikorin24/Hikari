#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Effective;
using Elffy.Imaging;
using Elffy.Mathematics;
using Elffy.UI;
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
        //Environment.SetEnvironmentVariable("RUST_LOG", "INFO");
        var screenConfig = new ScreenConfig
        {
            Backend = GraphicsBackend.Dx12,
            Width = 1280,
            Height = 720,
            Style = WindowStyle.Default,
            PresentMode = SurfacePresentMode.VsyncOn,
        };
        Engine.Run(screenConfig, OnInitialized);
    }

    private static void OnInitialized(Screen screen)
    {
        var panel = Serializer.Deserialize<Panel>("""
        {
            "@type": "panel",
            "width": 600,
            "height": "80%",
            "horizontalAlignment": "Center",
            "padding": "10px 80px",
            "backgroundColor": "#ffee23",
            "borderWidth": "10px",
            "borderColor": "#22B7FF",
            "borderRadius": 120,
            "children":
            [{
                "@type": "button",
                "horizontalAlignment": "Center",
                "backgroundColor": "#22B7FF",
                "width": 250,
                "height": 150,
                "borderWidth": 1
            },
            {
                "@type": "button",
                "width": 80,
                "height": 100,
                "borderWidth": 1,
                "backgroundColor": "#9622FF"
            }]
        }
        """);

        var uiLayer = new UILayer(screen, 2);
        uiLayer.AddRootElement(panel);

        screen.Update.Subscribe(screen =>
        {
            if(screen.FrameNum % 60 == 0) {
                var button = panel.Children[1];
                if(button.HorizontalAlignment == HorizontalAlignment.Left) {
                    button.HorizontalAlignment = HorizontalAlignment.Right;
                }
                else {
                    button.HorizontalAlignment = HorizontalAlignment.Left;
                }
            }
        });

        screen.Title = "sample";
        var layer = new PbrLayer(screen, 0);
        var deferredProcess = new DeferredProcess(layer, 1);

        var albedo = LoadTexture(screen, "resources/ground_0036_color_1k.jpg", true);
        var mr = LoadRoughnessAOTexture(screen, "resources/ground_0036_roughness_1k.jpg", "resources/ground_0036_ao_1k.jpg");
        var normal = LoadTexture(screen, "resources/ground_0036_normal_opengl_1k.png", false);

        var model = new PbrModel(layer, Shapes.Plane(screen, true), albedo, mr, normal);
        model.Rotation = Quaternion.FromAxisAngle(Vector3.UnitX, -90.ToRadian());
        model.Scale = 10;
        var material = model.Material;
        var cube = new PbrModel(layer, Shapes.Cube(screen, true),
            material.Albedo, material.MetallicRoughness, material.Normal);
        cube.Scale = 0.3f;
        cube.Position = new Vector3(0, 0.2f, 0);

        var camera = screen.Camera;
        camera.SetNearFar(0.5f, 1000);
        camera.LookAt(Vector3.Zero, new Vector3(0, 2f, 3) * 0.6f);

        screen.Lights.DirectionalLight.SetLightData(new Vector3(-0.5f, -1, 0), Color3.White);

        var sw = new Stopwatch();
        sw.Start();
        var elapsed = TimeSpan.Zero;
        var sum = TimeSpan.Zero;
        var N = 100;
        screen.Update.Subscribe(screen =>
        {
            System.Threading.Thread.Sleep(11);
            var tmp = sw.Elapsed;
            var delta = tmp - elapsed;
            elapsed = tmp;
            sum += delta;
            if(screen.FrameNum % (ulong)N == 0) {
                var fps = 1.0 / (sum / N).TotalSeconds;
                Console.WriteLine($"{fps:N1}");
                sum = TimeSpan.Zero;
            }

            var a = (screen.FrameNum * 10f / 360f).ToRadian();
            //var a = (360 * (float)delta.TotalSeconds).ToRadian();
            cube.Rotation = Quaternion.FromAxisAngle(Vector3.UnitY, a);
            //model.Rotation = Quaternion.FromAxisAngle(Vector3.UnitY, -a) * Quaternion.FromAxisAngle(Vector3.UnitX, -90.ToRadian());
        });

        screen.Update.Subscribe(screen =>
        {
            ControlCamera(screen.Mouse, camera, Vector3.Zero);
        });

        material.Albedo.ReadCallback((data, texture) =>
        {
            var image = new ReadOnlyImageRef(data.MarshalCast<byte, ColorByte>(), (int)texture.Width, (int)texture.Height);
            //image.SaveAsPng("test.png");
        });

        cube.Mesh.VertexBuffer.ReadCallback((bytes, _) =>
        {
            var vertices = bytes.MarshalCast<byte, Vertex>();
            return;
        });

        var mesh = cube.Mesh;
        cube.Mesh.IndexBuffer.ReadCallback((bytes, _) =>
        {
            var indices = bytes.MarshalCast<byte, ushort>();
            for(int i = 0; i < indices.Length / 3; i++) {
                Debug.WriteLine($"{indices[i * 3 + 0]}, {indices[i * 3 + 1]}, {indices[i * 3 + 2]}");
            }
        });
    }

    private static Own<Texture> LoadTexture(Screen screen, string filepath, bool isSrgb)
    {
        var format = isSrgb ? TextureFormat.Rgba8UnormSrgb : TextureFormat.Rgba8Unorm;
        using var image = LoadImage(filepath);
        return Texture.CreateWithAutoMipmap(screen, image, format, TextureUsages.TextureBinding | TextureUsages.CopySrc);

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
        return Texture.CreateWithAutoMipmap(screen, image, format, TextureUsages.TextureBinding | TextureUsages.CopySrc);

        static Image LoadImage(string filepath)
        {
            using var stream = File.OpenRead(filepath);
            return Image.FromStream(stream, Path.GetExtension(filepath));
        }
    }

    private static void ControlCamera(Mouse mouse, Camera camera, Vector3 target)
    {
        var cameraPos = camera.Position;
        var posChanged = false;
        if(mouse.IsPressed(MouseButton.Left)) {
            var vec = mouse.PositionDelta * (MathTool.PiOver180 * 0.5f);
            //vec.Y = 0;
            cameraPos = CalcCameraPosition(cameraPos, target, vec.X, vec.Y);
            posChanged = true;
        }

        var wheelDelta = mouse.WheelDelta;
        if(wheelDelta != 0) {
            cameraPos += (cameraPos - target) * wheelDelta * -0.1f;
            posChanged = true;
        }

        if(posChanged) {
            camera.LookAt(target, cameraPos);
        }
    }

    private static Vector3 CalcCameraPosition(in Vector3 cameraPos, in Vector3 center, float horizontalAngle, float verticalAngle)
    {
        const float MaxVertical = 89.99f * MathTool.PiOver180;
        const float MinVertical = -MaxVertical;
        var vec = cameraPos - center;
        var radius = vec.Length;
        var xzLength = vec.Xz.Length;
        var beta = MathF.Atan2(vec.Y, xzLength) + verticalAngle;
        beta = MathF.Max(MathF.Min(beta, MaxVertical), MinVertical);

        Vector3 result;
        var (sinBeta, cosBeta) = MathF.SinCos(beta);
        (result.X, result.Z) = Matrix2.GetRotation(horizontalAngle) * vec.Xz * (radius * cosBeta / xzLength);
        result.Y = radius * sinBeta;
        return result + center;
    }
}
