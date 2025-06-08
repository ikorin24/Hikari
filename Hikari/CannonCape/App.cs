using Cysharp.Threading.Tasks;
using Hikari;
using Hikari.Mathematics;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CannonCape;

public static class App
{
    private static Screen? _screen;
    private static PbrShader? _pbrShader;
    private static Input? _input;
    private static readonly Stopwatch _sw = Stopwatch.StartNew();

    public static Screen Screen => _screen!;
    public static Camera Camera => _screen!.Camera;
    public static Input Input => _input!;
    public static PbrShader PbrShader => _pbrShader!;

    public static TimeSpan CurrentTime => _sw.Elapsed;

    [STAThread]
    private static void Main()
    {
        Environment.SetEnvironmentVariable("RUST_BACKTRACE", "1");
        var backend =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? GraphicsBackend.Dx12 :
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? GraphicsBackend.Metal :
            GraphicsBackend.Vulkan;
        var screenConfig = new ScreenConfig
        {
            Backend = backend,
            Width = 1920,
            Height = 1080,
            //Style = WindowStyle.Fullscreen,
            Style = WindowStyle.Default,
            PresentMode = SurfacePresentMode.VsyncOn,
        };
        Engine.Run(screenConfig, OnInitialized);
    }

    private static async UniTask OnInitialized(Screen screen)
    {
        screen.Title = "Cannon Cape";
        _screen = screen;
        _input = new Input(screen);
        _pbrShader = PbrShader.Create(screen).DisposeOn(screen.Closed);
        screen.RenderScheduler.SetDefaultRenderPass();
        screen.Lights.DirectionalLight.SetLightData(new Vector3(1f, -1f, -0.1f), Color3.White);
        await RunScenario();
        screen.RequestClose();
    }

    private static async UniTask RunScenario()
    {
        var state = ScenarioState.Home;
        var sea = CreateSea();
        var sky = CreateSky();
        try {
            while(true) {
                switch(state) {
                    default:
                    case ScenarioState.Home: {
                        state = await HomeScene.Run();
                        continue;
                    }
                    case ScenarioState.MainPlay:
                        state = await MainPlayScene.Run();
                        continue;
                    case ScenarioState.Quit: {
                        return;
                    }
                }
            }
        }
        finally {
            sea.Terminate();
            sky.Terminate();
        }
    }

    private static FrameObject CreateSky()
    {
        var screen = App.Screen;
        var shader = SkyShader.Create(screen).DisposeOn(screen.Closed);
        var material = SkyMaterial.Create(shader).DisposeOn(screen.Closed);
        var mesh = PrimitiveShapes.SkySphere(screen, false).DisposeOn(screen.Closed);
        return new FrameObject(mesh, material)
        {
            Name = "sky",
            Scale = new Vector3(1200),
        };
    }

    private static FrameObject CreateSea()
    {
        var screen = App.Screen;
        var albedo = Texture2D.Create1x1Rgba8UnormSrgb(screen, TextureUsages.TextureBinding, new ColorByte(45, 55, 110, 255)).DisposeOn(screen.Closed);
        var metallicRoughness = Texture2D.Create1x1Rgba8Unorm(screen, TextureUsages.TextureBinding, new ColorByte(0, 127, 0, 0)).DisposeOn(screen.Closed);
        var normal = Texture2D.Create1x1Rgba8Unorm(screen, TextureUsages.TextureBinding, new ColorByte(127, 127, 255, 255)).DisposeOn(screen.Closed);
        var material = PbrMaterial.Create(App.PbrShader, albedo, metallicRoughness, normal).DisposeOn(screen.Closed);
        var mesh = PrimitiveShapes.Plane(screen, true).DisposeOn(screen.Closed);
        return new FrameObject(mesh, material)
        {
            Name = "sea",
            Rotation = Quaternion.FromAxisAngle(Vector3.UnitX, -90.ToRadian()),
            Scale = new Vector3(1200),
        };
    }
}

public enum ScenarioState
{
    Home,
    MainPlay,
    Quit,
}
