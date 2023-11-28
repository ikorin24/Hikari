#nullable enable
using Cysharp.Threading.Tasks;
using System.Linq;
using Hikari;
using System;
using System.Runtime.CompilerServices;
using Hikari.Imaging;

namespace SampleApp;

public sealed class App
{
    public required Screen Screen { get; init; }
    public required PbrShader PbrBasicShader { get; init; }
    public required DeferredProcessShader DeferredProcessShader { get; init; }

    public static App BuildPipelines(Screen screen)
    {
        var gBuffer = GBufferProvider.Create(screen, screen.ClientSize,
        [
            TextureFormat.Rgba32Float,
            TextureFormat.Rgba32Float,
            TextureFormat.Rgba32Float,
            TextureFormat.Rgba32Float,
        ]).DisposeOn(screen.Closed);
        screen.Resized.Subscribe(x => gBuffer.Resize(x.Size));

        screen.Scheduler.SetRenderPass([
            .. screen.Lights.DirectionalLight.PrepareShadowMapPassDefinitions,
            new RenderPassDefinition
            {
                Kind = PassKind.GBuffer,
                UserData = gBuffer,
                Factory = static (screen, gBuffer) =>
                {
                    return RenderPass.Create(
                        screen,
                        SafeCast.NotNullAs<IGBufferProvider>(gBuffer),
                        static (colors, gBuffer) =>
                        {
                            var textures = gBuffer.Textures;
                            for(int i = 0; i < textures.Length; i++) {
                                colors[i] = new ColorAttachment
                                {
                                    Target = textures[i],
                                    LoadOp = ColorBufferLoadOp.Clear(),
                                };
                            }
                        },
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
                    //// TODO: remove
                    //if(screen.Keyboard.IsUp(Keys.Space)) {
                    //    var shadowMap = screen.Lights.DirectionalLight.ShadowMap;
                    //    shadowMap
                    //        .ReadToArray()
                    //        .ContinueWith(bytes =>
                    //        {
                    //            var pixels = bytes.AsSpan().MarshalCast<byte, (Half R, Half G)>().ToArray().Select(x => new Color4((float)x.R, (float)x.G, 0, 1f).ToColorByte()).ToArray().AsSpan();
                    //            var image = new ImageView(pixels, shadowMap.Width, shadowMap.Height);
                    //            image.SaveAsPng("shadowmap.png");
                    //        }).Forget();

                    //    var vsmt = screen.Lights.DirectionalLight.VarianceShadowMapTexture;
                    //    vsmt
                    //        .ReadToArray()
                    //        .ContinueWith(bytes =>
                    //        {
                    //            var pixels = bytes.AsSpan().MarshalCast<byte, (Half R, Half G)>().ToArray().Select(x => new Color4((float)x.R, (float)x.G, 0, 1f).ToColorByte()).ToArray().AsSpan();
                    //            var image = new ImageView(pixels, vsmt.Width, vsmt.Height);
                    //            image.SaveAsPng("shadowmap_vsm.png");
                    //        }).Forget();
                    //}

                    return RenderPass.Create(
                        screen,
                        new ColorAttachment
                        {
                            Target = screen.Surface,
                            LoadOp = ColorBufferLoadOp.Clear(),
                        },
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
        VarianceShadowMapper.Create(screen);
        return app;
    }
}

