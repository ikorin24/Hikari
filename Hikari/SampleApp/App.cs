#nullable enable
using Hikari;

namespace SampleApp;

public sealed class App
{
    public required Screen Screen { get; init; }
    public required PbrShader PbrBasicShader { get; init; }
    public required DeferredProcessShader DeferredProcessShader { get; init; }

    public static App BuildPipelines(Screen screen)
    {
        var gBuffer = GBufferProvider.CreateScreenSize(screen,
        [
            TextureFormat.Rgba32Float,
            TextureFormat.Rgba32Float,
            TextureFormat.Rgba32Float,
            TextureFormat.Rgba32Float,
        ]).DisposeOn(screen.Closed);

        screen.Scheduler.SetRenderPass([
            .. screen.Lights.DirectionalLight.ShadowMapPassDefinitions,
            new RenderPassDefinition
            {
                Kind = PassKind.GBuffer,
                UserData = gBuffer,
                Factory = static (screen, gBuffer) =>
                {
                    var textures = SafeCast.NotNullAs<IGBufferProvider>(gBuffer).GetCurrentGBuffer().Textures;
                    return RenderPass.Create(
                        screen,
                        [
                            new ColorAttachment { Target = textures[0], LoadOp = ColorBufferLoadOp.Clear(), },
                            new ColorAttachment { Target = textures[1], LoadOp = ColorBufferLoadOp.Clear(), },
                            new ColorAttachment { Target = textures[2], LoadOp = ColorBufferLoadOp.Clear(), },
                            new ColorAttachment { Target = textures[3], LoadOp = ColorBufferLoadOp.Clear(), },
                        ],
                        new DepthStencilAttachment
                        {
                            Target = screen.DepthStencil,
                            LoadOp = new DepthStencilBufferLoadOp
                            {
                                Depth = DepthBufferLoadOp.Clear(0f),
                                Stencil = null,
                            },
                        });
                }
            },
            new RenderPassDefinition
            {
                Kind = PassKind.Surface,
                Factory = static (screen, _) =>
                {
                    return RenderPass.Create(
                        screen,
                        new ColorAttachment { Target = screen.Surface, LoadOp = ColorBufferLoadOp.Clear(), },
                        new DepthStencilAttachment
                        {
                            Target = screen.DepthStencil,
                            LoadOp = new DepthStencilBufferLoadOp
                            {
                                Depth = DepthBufferLoadOp.Clear(0f),
                                Stencil = null,
                            },
                        });
                },
            },
        ]);

        var app = new App
        {
            Screen = screen,
            PbrBasicShader = PbrShader.Create(screen, gBuffer).DisposeOn(screen.Closed),
            DeferredProcessShader = DeferredProcessShader.Create(screen).DisposeOn(screen.Closed),
        };
        DeferredPlane.AddRenderer(app.DeferredProcessShader, gBuffer);
        return app;
    }
}

