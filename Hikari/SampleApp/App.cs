#nullable enable
using Hikari;
using Hikari.UI;

namespace SampleApp;

public sealed class App
{
    private readonly Screen _screen;
    private readonly PbrLayer _pbrLayer;
    private readonly DeferredProcess _deferredProcess;
    private readonly UITree _ui;

    private readonly PbrShader _pbrShader;

    public Screen Screen => _screen;
    public PbrLayer PbrLayer => _pbrLayer;
    public DeferredProcess DeferredProcess => _deferredProcess;
    public UITree UI => _ui;

    public PbrShader PbrShader => _pbrShader;


    private App(Screen screen, PbrLayer pbrLayer, DeferredProcess deferredProcess, UITree ui)
    {
        _screen = screen;
        _pbrLayer = pbrLayer;
        _deferredProcess = deferredProcess;
        _ui = ui;
        _pbrShader = PbrShader.Create(screen, pbrLayer).DisposeOn(pbrLayer.Dead);
    }

    public static App BuildPipelines(Screen screen)
    {
        var ops = screen.Operations;
        var gBufferProvider = GBufferProvider.Create(screen, screen.ClientSize, stackalloc TextureFormat[4]
        {
            TextureFormat.Rgba32Float,
            TextureFormat.Rgba32Float,
            TextureFormat.Rgba32Float,
            TextureFormat.Rgba32Float,
        }).DisposeOn(screen.Closed);
        screen.Resized.Subscribe(x => gBufferProvider.Resize(x.Size));
        var pbrLayer = ops.AddPbrLayer(0, new PbrLayerDescriptor
        {
            InputGBuffer = gBufferProvider,
            DepthStencilFormat = screen.DepthStencil.Format,
            OnRenderPass = static layer =>
            {
                return RenderPass.Create(
                    layer.Screen,
                    layer.InputGBuffer,
                    static (colors, gBuffer) =>
                    {
                        for(int i = 0; i < gBuffer.ColorAttachmentCount; i++) {
                            colors[i] = new ColorAttachment
                            {
                                Target = gBuffer[i],
                                LoadOp = ColorBufferLoadOp.Clear(),
                            };
                        }
                    },
                    new DepthStencilAttachment
                    {
                        Target = layer.Screen.DepthStencil,
                        LoadOp = new DepthStencilBufferLoadOp
                        {
                            Depth = DepthBufferLoadOp.Clear(0f),
                            Stencil = null,
                        },
                    });
            },
        });
        var deferredProcess = ops.AddDeferredProcess(1, new DeferredProcessDescriptor
        {
            InputGBuffer = gBufferProvider,
            ColorFormat = screen.Surface.Format,
            DepthStencilFormat = screen.DepthStencil.Format,
            OnRenderPass = static self =>
            {
                return RenderPass.Create(
                    self.Screen,
                    new ColorAttachment
                    {
                        Target = self.Screen.Surface,
                        LoadOp = ColorBufferLoadOp.Clear(),
                    },
                    new DepthStencilAttachment
                    {
                        Target = self.Screen.DepthStencil,
                        LoadOp = new DepthStencilBufferLoadOp
                        {
                            Depth = DepthBufferLoadOp.Clear(0f),
                            Stencil = null,
                        },
                    });
            },
        });
        var ui = ops.AddUI(2, new UIDescriptor
        {
            ColorFormat = screen.Surface.Format,
            DepthStencilFormat = screen.DepthStencil.Format,
            OnRenderPass = static screen =>
            {
                return RenderPass.Create(
                    screen,
                    new ColorAttachment
                    {
                        Target = screen.Surface,
                        LoadOp = ColorBufferLoadOp.Load(),
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
        });

        return new App(screen, pbrLayer, deferredProcess, ui);
    }
}
