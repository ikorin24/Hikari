using Cysharp.Threading.Tasks;
using Hikari;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CannonCape;

public static class App
{
    private static Screen? _screen;
    private static PbrShader? _pbrShader;
    private static Input? _input;
    private static Stopwatch _sw = Stopwatch.StartNew();

    public static Screen Screen => _screen!;
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
        screen.Title = "キャノンケープ";
        _screen = screen;
        _input = new Input(screen);
        _pbrShader = PbrShader.Create(screen).DisposeOn(screen.Closed);
        screen.RenderScheduler.SetDefaultRenderPass();
        var scenario = new Scenario();
        await scenario.Start();
    }
}
