#nullable enable
using Hikari.Imaging;
using Hikari.Mathematics;
using Hikari;
using Hikari.UI;
using System;
using System.Diagnostics;
using System.IO;
using Hikari.Gltf;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace SampleApp;

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
        screen.Update.Subscribe(_ => System.Threading.Thread.Sleep(16 * 3));
        var app = App.BuildPipelines(screen);
        screen.Title = "sample";

        screen.UITree.RenderRoot($$"""
        {
            "@type": {{typeof(Counter)}},
            "Width": "600px",
            "Height": "300px"
        }
        """);
        //var button = new Button
        //{
        //    Width = 100,
        //    Height = 100,
        //    Text = "hoge",
        //    Background = Brush.White,
        //};
        //button.Clicked.Subscribe(button =>
        //{
        //    Debug.WriteLine("clicked");
        //});
        //screen.UITree.SetRoot(button);

        var model = GlbModelLoader.LoadGlbFile(app.PbrBasicShader, @"D:\private\source\Elffy\src\Sandbox\Resources\AntiqueCamera.glb");
        //var model = GlbModelLoader.LoadGlbFile(app.PbrBasicShader, @"C:\Users\ikorin\Downloads\2CylinderEngine.glb");
        //var model = GlbModelLoader.LoadGlbFile(app.PbrBasicShader, @"C:\Users\ikorin\Downloads\BarramundiFish.glb");
        //var model = GlbModelLoader.LoadGlbFile(app.PbrBasicShader, @"C:\Users\ikorin\Downloads\BoomBox.glb");
        //var model = GlbModelLoader.LoadGlbFile(app.PbrBasicShader, @"C:\Users\ikorin\Downloads\Buggy.glb");
        //var model = GlbModelLoader.LoadGlbFile(app.PbrBasicShader, @"C:\Users\ikorin\Downloads\Fox.glb");
        //var model = GlbModelLoader.LoadGlbFile(app.PbrBasicShader, @"C:\Users\ikorin\Downloads\CesiumMan.glb");
        //var model = GlbModelLoader.LoadGlbFile(app.PbrBasicShader, @"C:\Users\ikorin\Downloads\Avocado.glb");

        var tasks = model
            .GetDescendants()
            .OfType<FrameObject>()
            .Select(m =>
            {
                return m.Renderer.Mesh.VertexBuffer.ReadToArray().ContinueWith(x => x.AsSpan().MarshalCast<byte, Vertex>().ToArray());
            });
        UniTask.WhenAll(tasks).ContinueWith(result =>
        {
            var vertices = result.SelectMany(v => v).Select(v => v.Position).ToArray();
            var min = vertices.Aggregate((a, b) => Vector3.Min(a, b));
            var max = vertices.Aggregate((a, b) => Vector3.Max(a, b));
            var len = (max - min).Length;
            model.Scale = new Vector3(1f / len * 10);
        }).Forget();

        var albedo = LoadTexture(screen, "resources/ground_0036_color_1k.jpg", true);
        var mr = LoadRoughnessAOTexture(screen, "resources/ground_0036_roughness_1k.jpg", "resources/ground_0036_ao_1k.jpg");
        var normal = LoadTexture(screen, "resources/ground_0036_normal_opengl_1k.png", false);
        var plane = new FrameObject(Shapes.Plane(screen, true), PbrMaterial.Create(app.PbrBasicShader, albedo, mr, normal).Cast<Material>());
        plane.Rotation = Quaternion.FromAxisAngle(Vector3.UnitX, -90.ToRadian());
        plane.Scale = new Vector3(16);

        var camera = screen.Camera;
        camera.SetNearFar(0.5f, 1000);
        camera.LookAt(Vector3.Zero, new Vector3(0, 2f, 3) * 0.6f);
        screen.Update.Subscribe(screen =>
        {
            ControlCamera(screen.Mouse, camera, Vector3.Zero);
        });
        screen.Lights.DirectionalLight.SetLightData(new Vector3(0.5f, -1, -1.5f), Color3.White);
        //screen.Lights.DirectionalLight.SetLightData(new Vector3(0, -1, -8.5f), Color3.White);
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

    private static void ControlCamera(Mouse mouse, Camera camera, Vector3 target)
    {
        var cameraPos = camera.Position;
        var posChanged = false;
        if(mouse.IsPressed(MouseButton.Left)) {
            var vec = (mouse.PositionDelta ?? Vector2.Zero) * ((float.Pi / 180f) * 0.5f);
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

[ReactComponent]
public partial class Counter
{
    private partial record struct Props(
        LayoutLength Width,
        LayoutLength Height
    );
    private int _countA;
    private int _countB;
    private int _countC;
    private partial ObjectSourceBuilder Render(in Props props)
    {
        var text = $"A: {_countA}, B: {_countB}, C: {_countC}";
        return $$"""
        {
            "@type": {{typeof(Panel)}},
            "Width": {{props.Width}},
            "Height": {{props.Height}},
            "Background": "#ddd",
            "BorderRadius": "10px",
            "Flow": "Column NoWrap",
            "Children": [
            {
                "@type": {{typeof(Label)}},
                "@key": "0",
                "VerticalAlignment": "Top",
                "Height": 80,
                "FontSize": 20,
                "Background": "#27acd9",
                "BorderRadius": "10px 10px 0px 0px",
                "Color": "#fff",
                "Text": {{text}}
            },
            {
                "@type": {{typeof(Panel)}},
                "Background": "#0000",
                "Flow": "Row Wrap",
                "Children": [
                {
                    "@type": {{typeof(CountButton)}},
                    "@key": "1",
                    "Width": 150,
                    "Height": 60,
                    "Clicked": {{(UIElement _) =>
                    {
                        SetState(ref _countA, _countA + 1);
                    }}}
                },
                {
                    "@type": {{typeof(CountButton)}},
                    "@key": "2",
                    "Width": 150,
                    "Height": 60,
                    "Clicked": {{(UIElement _) =>
                    {
                        SetState(ref _countB, _countB + 1);
                    }}}
                },
                {
                    "@type": {{typeof(CountButton)}},
                    "@key": "3",
                    "Width": 150,
                    "Height": 60,
                    "Clicked": {{(UIElement _) =>
                    {
                        SetState(ref _countC, _countC + 1);
                    }}}
                }]
            }]
        }
        """;
    }

    partial void OnMount()
    {
    }

    partial void OnUnmount()
    {
    }
}

[ReactComponent]
public partial class CountButton
{
    private partial record struct Props(int Width, int Height, Action<Button> Clicked);

    private partial ObjectSourceBuilder Render(in Props props)
    {
        return $$"""
        {
            "@type": {{typeof(Button)}},
            "Width": {{props.Width}},
            "Height": {{props.Height}},
            "BorderRadius": {{props.Height / 2f}},
            "BorderColor": "#27acd9",
            "BorderWidth": 4,
            "Background": "#fff",
            "Margin": 4,
            "Text": "click me!",
            "FontSize": 16,
            "Color": "#27acd9",
            "BoxShadow": "0px 0px 4px 0px #000e",
            "Clicked": {{props.Clicked}},
            "&:Hover": {
                "Background": "#27acd9",
                "Color": "#fff",
            },
            "&:Active": {
                "BoxShadow": "0px 0px 2px 0px #000e",
                "Background": "#1089d9",
                "BorderColor": "#1089d9",
                "Color": "#fff",
            }
        }
        """;
    }
}

sealed partial class Counter
    : IReactComponent,
      IFromJson<Counter>
{
    readonly partial record struct Props;

    private Props __p;
    private bool _needsToRerender;
    private object? _rendered;

    static Counter()
    {
        Serializer.RegisterConstructor(FromJson);
    }

    private Counter(Props props)
    {
        __p = props;
    }

    bool IReactComponent.NeedsToRerender => _needsToRerender;

    private partial ObjectSourceBuilder Render(in Props props);

    private void SetState<T>(ref T state, in T newValue)
    {
        state = newValue;
        _needsToRerender = true;
    }

    public static Counter FromJson(in ObjectSource source)
    {
        var props = Props.FromJson(source);
        return new Counter(props);
    }

    ObjectSource IReactComponent.GetSource() => Render(__p).ToSourceClear();

    void IReactComponent.RenderCompleted<T>(T rendered)
    {
        _rendered = rendered;
        _needsToRerender = false;
    }

    void IReactive.ApplyDiff(in ObjectSource source)
    {
        __p = Props.FromJson(source);
        var innerSource = Render(__p).ToSourceClear();
        _rendered = innerSource.Apply(_rendered, out _);
    }

    partial void OnMount();
    partial void OnUnmount();

    void IReactComponent.OnMount() => OnMount();
    void IReactComponent.OnUnmount() => OnUnmount();

    partial record struct Props : IFromJson<Props>
    {
        static Props()
        {
            Serializer.RegisterConstructor(FromJson);
        }

        public static Props FromJson(in ObjectSource source)
        {
            return new()
            {
                //Message = element.TryGetProperty("Message"u8, out var message) ? Serializer.Instantiate<string>(message) : "",
                Width = source.TryGetProperty(nameof(Width), out var width) ? width.Instantiate<LayoutLength>() : default,
                Height = source.TryGetProperty(nameof(Height), out var height) ? height.Instantiate<LayoutLength>() : default,
            };
        }
    }
}

sealed partial class CountButton : IReactComponent, IFromJson<CountButton>
{
    readonly partial record struct Props;

    private Props __p;
    private bool _needsToRerender;
    private object? _rendered;

    static CountButton()
    {
        Serializer.RegisterConstructor(FromJson);
    }

    private CountButton(Props props)
    {
        __p = props;
    }

    bool IReactComponent.NeedsToRerender => _needsToRerender;

    private partial ObjectSourceBuilder Render(in Props props);

    private void SetState<T>(ref T state, in T newValue)
    {
        state = newValue;
        _needsToRerender = true;
    }

    public static CountButton FromJson(in ObjectSource source)
    {
        var props = Props.FromJson(source);
        return new CountButton(props);
    }

    ObjectSource IReactComponent.GetSource() => Render(__p).ToSourceClear();

    void IReactComponent.RenderCompleted<T>(T rendered)
    {
        _rendered = rendered;
        _needsToRerender = false;
    }

    void IReactive.ApplyDiff(in ObjectSource source)
    {
        __p = Props.FromJson(source);
        var innerSource = Render(__p).ToSourceClear();
        _rendered = innerSource.Apply(_rendered, out _);
    }

    partial void OnMount();
    partial void OnUnmount();

    void IReactComponent.OnMount() => OnMount();
    void IReactComponent.OnUnmount() => OnUnmount();

    partial record struct Props : IFromJson<Props>
    {
        static Props()
        {
            Serializer.RegisterConstructor(FromJson);
        }

        public static Props FromJson(in ObjectSource source)
        {
            return new()
            {
                Width = source.TryGetProperty(nameof(Width), out var width) ? width.Instantiate<int>() : default,
                Height = source.TryGetProperty(nameof(Height), out var height) ? height.Instantiate<int>() : default,
                Clicked = source.TryGetProperty(nameof(Clicked), out var clicked) ? clicked.Instantiate<Action<Button>>() : throw new ArgumentNullException("Clicked"),
            };
        }
    }
}
