#nullable enable
using Hikari;

namespace SampleApp;

public sealed class App
{
    public required Screen Screen { get; init; }
    public required PbrBasicShader PbrBasicShader { get; init; }
    public required DeferredProcessShader DeferredProcessShader { get; init; }

    public static App BuildPipelines(Screen screen)
    {
        var gBuffer = GBufferProvider.Create(screen, screen.ClientSize, stackalloc TextureFormat[4]
        {
            TextureFormat.Rgba32Float,
            TextureFormat.Rgba32Float,
            TextureFormat.Rgba32Float,
            TextureFormat.Rgba32Float,
        }).DisposeOn(screen.Closed);
        screen.Resized.Subscribe(x => gBuffer.Resize(x.Size));
        var app = new App
        {
            Screen = screen,
            PbrBasicShader = PbrBasicShader.Create(screen, gBuffer).DisposeOn(screen.Closed),
            DeferredProcessShader = DeferredProcessShader.Create(screen).DisposeOn(screen.Closed),
        };
        _ = new DeferredPlane(app.DeferredProcessShader, gBuffer);
        return app;
    }
}

