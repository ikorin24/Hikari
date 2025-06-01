#nullable enable
using Cysharp.Threading.Tasks;
using Hikari;
using Hikari.Gltf;
using Hikari.Imaging;
using Hikari.Mathematics;
using Hikari.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SampleApp;

internal class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Environment.SetEnvironmentVariable("RUST_BACKTRACE", "1");
        var screenConfig = new ScreenConfig
        {
            Backend = GraphicsBackend.Dx12,
            Width = 1920,
            Height = 1080,
            Style = WindowStyle.Default,
            PresentMode = SurfacePresentMode.VsyncOn,
        };
        Engine.Run(screenConfig, OnInitialized);
    }

    private static void SetLight(Screen screen, float angle)
    {
        var (sin, cos) = MathF.SinCos(angle);
        var vec = -new Vector3(sin * 10, 10, cos * 2);
        screen.Lights.DirectionalLight.SetLightData(vec.Normalized(), Color3.White);
    }

    private static async void OnInitialized(Screen screen)
    {
        var app = App.BuildPipelines(screen);
        screen.Title = "SampleApp";

        var camera = screen.Camera;
        camera.SetNearFar(0.5f, 1000);
        camera.LookAt(new Vector3(0, 0.5f, 0), new Vector3(0, 1f, 3) * 2.0f);
        screen.Update.Subscribe(ControlCamera);

        var angle = 0.ToRadian() * 0.3f;
        var (sin, cos) = MathF.SinCos(angle);
        var vec = -new Vector3(sin * 10, 10, cos * 2);
        screen.Lights.DirectionalLight.SetLightData(vec.Normalized(), Color3.White);
        SetLight(screen, 0);
        screen.Lights.AmbientStrength = 0.1f;

        FrameObject cameraModel = null!;
        await UniTask.WhenAll(
            UniTask.Run(() =>
            {
                var model = GlbModelLoader.LoadGlbFile(app.PbrBasicShader, @"resources\AntiqueCamera.glb");
                model.Position = new Vector3(0, 0, 0);
                model.Scale = new Vector3(0.2f);
                cameraModel = model;
            }),
            UniTask.Run(() =>
            {
                var model2 = GlbModelLoader.LoadGlbFile(app.PbrBasicShader, @"resources\Avocado.glb");
                model2.Position = new Vector3(0, 0, -1.3f);
                model2.Scale = new Vector3(25f);
            }),
            UniTask.Run(() =>
            {
                var albedo = LoadTexture(screen, "resources/ground_0036_color_1k.jpg", true);
                var mr = LoadRoughnessAOTexture(screen, "resources/ground_0036_roughness_1k.jpg", "resources/ground_0036_ao_1k.jpg");
                var normal = LoadTexture(screen, "resources/ground_0036_normal_opengl_1k.png", false);
                var plane = new FrameObject(
                    Shapes.Plane(screen, true),
                    PbrMaterial.Create(app.PbrBasicShader, albedo, mr, normal).Cast<IMaterial>());
                plane.Rotation = Quaternion.FromAxisAngle(Vector3.UnitX, -90.ToRadian());
                plane.Scale = new Vector3(3);
            }));
        Debug.WriteLine("all loaded");
        await screen.Update.Switch();

        var rand = new Xorshift32();
        var avocados = new Queue<FrameObject>();
        bool loading = false;
        var createAvocado = () =>
        {
            if(loading) {
                return;
            }
            UniTask.Void(async () =>
            {
                loading = true;
                try {
                    FrameObject avocado;
                    if(avocados.Count <= 4) {
                        avocado = await UniTask.Run(() =>
                        {
                            return GlbModelLoader.LoadGlbFile(app.PbrBasicShader, @"resources\Avocado.glb");
                        });
                        Debug.WriteLine("create avocado");
                        avocado.Scale = new Vector3(5f);
                        avocados.Enqueue(avocado);
                    }
                    else {
                        avocado = avocados.Dequeue();
                        avocados.Enqueue(avocado);
                    }
                    avocado.Position = new Vector3
                    {
                        X = rand.Single() * 2 - 1,
                        Y = rand.Single() * 0.2f + 0.2f,
                        Z = rand.Single() * 2 - 1,
                    };
                    avocado.Rotation = Quaternion.FromAxisAngle(
                        new Vector3
                        {
                            X = rand.Single() + 0.0001f,
                            Y = rand.Single(),
                            Z = rand.Single(),
                        }.Normalized(),
                        (rand.Single() * 360f).ToRadian());
                }
                finally {
                    loading = false;
                }
            });
        };
        Debug.WriteLine("create UI");
        CreateUI(screen, createAvocado);
        int i = 0;
        screen.Update.Subscribe(_ =>
        {
            var angle = i++.ToRadian() * 0.3f;
            SetLight(screen, angle);
        });
    }

    private static void CreateUI(Screen screen, Action buttonClicked)
    {
        var uiPanel = new Panel
        {
            Width = 160,
            Background = Brush.LinearGradient(0,
            [
                new(Color4.FromHexCode("#0000"), 0.6f),
                new(Color4.FromHexCode("#135B2D"), 0.9f),
            ]),
            HorizontalAlignment = HorizontalAlignment.Left,
            Flow = new Flow(FlowDirection.Column, FlowWrapMode.NoWrap),
            Height = LayoutLength.Proportion(1f),
            Children =
            [
                new Label
                {
                    Height = 30,
                    Text = "move: W,A,S,D, E,Q",
                    Color = Color4.White,
                    Background = Brush.Transparent,
                },
                new Label
                {
                    Height = 30,
                    Text = "rotate: left drag",
                    Color = Color4.White,
                    Background = Brush.Transparent,
                },
                new Button
                {
                    Height = 40,
                    Margin = new Thickness(15, 10),
                    Text = "Create Avocado",
                    Color = Color4.White,
                    BorderWidth = new Thickness(1f),
                    BorderColor = Brush.White,
                    BorderRadius = new CornerRadius(6),
                    Background = Brush.LinearGradient(0,
                    [
                        new(Color4.FromHexCode("#3075FF"), 0.75f),
                        new(Color4.FromHexCode("#5188FF"), 0.9f),
                    ]),
                    HoverProps = new()
                    {
                        Background = Brush.Solid(Color4.FromHexCode("#2565FF")),
                    },
                    ActiveProps = new()
                    {
                        Background = Brush.Solid(Color4.FromHexCode("#0055FF")),
                    },
                    OnClicked = self =>
                    {
                        buttonClicked.Invoke();
                    },
                },
            ],
        };
        screen.UITree.SetRoot(uiPanel);
    }

    private static Own<Texture2D> LoadTexture(Screen screen, string filepath, bool isSrgb)
    {
        var format = isSrgb ? TextureFormat.Rgba8UnormSrgb : TextureFormat.Rgba8Unorm;
        using var image = LoadImage(filepath);
        return Texture2D.CreateWithAutoMipmap(screen, image, format, TextureUsages.TextureBinding | TextureUsages.CopySrc);

        static Image LoadImage(string filepath)
        {
            using var stream = File.OpenRead(filepath);
            return Image.FromStream(stream, Path.GetExtension(filepath));
        }
    }

    private static Own<Texture2D> LoadRoughnessAOTexture(Screen screen, string filepath, string aoFilePath)
    {
        var format = TextureFormat.Rgba8Unorm;
        using var image = LoadImage(filepath);
        using var aoImage = LoadImage(aoFilePath);
        var aoPixels = aoImage.GetPixels();

        var pixels = image.GetPixels();
        for(int i = 0; i < pixels.Length; i++) {
            pixels[i] = new ColorByte(0x00, pixels[i].G, aoPixels[i].R, 0x00);
        }
        return Texture2D.CreateWithAutoMipmap(screen, image, format, TextureUsages.TextureBinding | TextureUsages.CopySrc);

        static Image LoadImage(string filepath)
        {
            using var stream = File.OpenRead(filepath);
            return Image.FromStream(stream, Path.GetExtension(filepath));
        }
    }

    private static void ControlCamera(Screen screen)
    {
        var camera = screen.Camera;
        var keyboard = screen.Keyboard;
        var mouse = screen.Mouse;
        var cameraPos = camera.Position;
        var target = cameraPos + camera.Direction * 2f;
        var posChanged = false;
        if(keyboard.IsPressed(KeyCode.KeyW) || keyboard.IsPressed(KeyCode.KeyA) || keyboard.IsPressed(KeyCode.KeyS) || keyboard.IsPressed(KeyCode.KeyD)
                || keyboard.IsPressed(KeyCode.KeyE) || keyboard.IsPressed(KeyCode.KeyQ)) {
            const float S = 0.04f;
            var v = camera.Direction * S;
            Vector3 vec = default;
            if(keyboard.IsPressed(KeyCode.KeyW)) {
                vec += v;
            }
            if(keyboard.IsPressed(KeyCode.KeyS)) {
                vec -= v;
            }
            if(keyboard.IsPressed(KeyCode.KeyA)) {
                var left = Matrix2.GetRotation(-90.ToRadian()) * v.Xz;
                vec += new Vector3(left.X, 0, left.Y);
            }
            if(keyboard.IsPressed(KeyCode.KeyD)) {
                var right = Matrix2.GetRotation(90.ToRadian()) * v.Xz;
                vec += new Vector3(right.X, 0, right.Y);
            }
            if(keyboard.IsPressed(KeyCode.KeyE)) {
                vec += Vector3.UnitY * S;
            }
            if(keyboard.IsPressed(KeyCode.KeyQ)) {
                vec -= Vector3.UnitY * S;
            }
            target += vec;
            cameraPos += vec;
            posChanged = true;
        }
        if(mouse.IsPressed(MouseButton.Left)) {
            var vec = (mouse.PositionDelta ?? Vector2.Zero) * ((float.Pi / 180f) * 0.05f);
            cameraPos = CalcCameraPosition(cameraPos, target, vec.X, vec.Y);
            posChanged = true;
        }

        //var wheelDelta = mouse.WheelDelta;
        //if(wheelDelta != 0) {
        //    cameraPos += (cameraPos - target) * wheelDelta * -0.1f;
        //    posChanged = true;
        //}

        if(posChanged) {
            camera.LookAt(target, cameraPos);
        }
    }

    private static Vector3 CalcCameraPosition(in Vector3 cameraPos, in Vector3 center, float horizontalAngle, float verticalAngle)
    {
        const float MaxVertical = 89.99f * (float.Pi / 180f);
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
