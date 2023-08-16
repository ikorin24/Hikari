#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.UI;

internal sealed class UILayer : ObjectLayer<UILayer, VertexSlim, UIShader, UIMaterial, UIModel>
{
    private readonly Own<BindGroupLayout> _bindGroupLayout0;
    private readonly Own<BindGroupLayout> _bindGroupLayout1;
    private readonly Own<BindGroupLayout> _bindGroupLayout2;
    private UIElement? _rootElement;
    private bool _isLayoutDirty = false;

    private readonly Dictionary<Type, Own<UIShader>> _shaderCache = new();
    private readonly object _shaderCacheLock = new();
    private bool _isShaderCacheAvailable = true;

    private static readonly ConcurrentDictionary<Type, Func<UILayer, Own<UIShader>>> _shaderFactory = new();

    public static bool RegisterShader<T>(Func<UILayer, Own<UIShader>> valueFactory) where T : UIElement
    {
        ArgumentNullException.ThrowIfNull(valueFactory);
        return _shaderFactory.TryAdd(typeof(T), valueFactory);
    }

    internal UIShader GetRegisteredShader(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        Own<UIShader> value;
        lock(_shaderCacheLock) {
            if(_isShaderCacheAvailable == false) {
                throw new InvalidOperationException("already dead");
            }
            if(_shaderCache.TryGetValue(type, out value) == false) {
                if(_shaderFactory.TryGetValue(type, out var factory) == false) {
                    Throw(type);
                }
                value = factory.Invoke(this);
                if(value.IsNone) {
                    Throw(type);
                }
                Dead.Subscribe(_ => value.Dispose()).AddTo(Subscriptions);
                _shaderCache.Add(type, value);
            }
        }
        return value.AsValue();

        [DoesNotReturn] static void Throw(Type type) => throw new ArgumentException($"shader for {type} is not found");
    }

    public BindGroupLayout BindGroupLayout0 => _bindGroupLayout0.AsValue();
    public BindGroupLayout BindGroupLayout1 => _bindGroupLayout1.AsValue();
    public BindGroupLayout BindGroupLayout2 => _bindGroupLayout2.AsValue();

    public UILayer(Screen screen, int sortOrder)
        : base(
            screen,
            CreatePipelineLayout(screen, out var bindGroupLayout0, out var bindGroupLayout1, out var bindGroupLayout2),
            sortOrder)
    {
        _bindGroupLayout0 = bindGroupLayout0;
        _bindGroupLayout1 = bindGroupLayout1;
        _bindGroupLayout2 = bindGroupLayout2;
        screen.Resized.Subscribe(arg =>
        {
            _isLayoutDirty = true;
        }).AddTo(Subscriptions);
        FrameInit.Subscribe(self =>
        {
            ((UILayer)self).UpdateLayout();
        }).AddTo(Subscriptions);
        Dead.Subscribe(_ =>
        {
            lock(_shaderCacheLock) {
                _isShaderCacheAvailable = false;
                _shaderCache.Clear();
            }
        }).AddTo(Subscriptions);
    }

    protected override void Release()
    {
        base.Release();
        _bindGroupLayout0.Dispose();
        _bindGroupLayout1.Dispose();
        _bindGroupLayout2.Dispose();
    }

    private void UpdateLayout()
    {
        var rootElement = _rootElement;
        if(rootElement == null) { return; }
        var screen = Screen;
        var screenSize = screen.ClientSize;
        var contentArea = new ContentAreaInfo
        {
            Rect = new RectF(Vector2.Zero, screenSize.ToVector2()),
            Padding = Thickness.Zero,
        };
        var isLayoutDirty = _isLayoutDirty;
        _isLayoutDirty = false;
        var mouse = screen.Mouse;
        rootElement.UpdateLayout(isLayoutDirty, contentArea, mouse);
    }

    public void SetRoot(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if(element.Parent != null) {
            throw new ArgumentException("the element is already in UI tree");
        }
        element.CreateModel(this);
        element.ModelAlive.Subscribe(model =>
        {
            _rootElement?.Model?.Terminate();
            _rootElement = model.Element;
        }).AddTo(Subscriptions);
    }

    protected override void RenderShadowMap(in RenderShadowMapContext context, ReadOnlySpan<UIModel> objects)
    {
    }

    protected override void Render(in RenderPass pass, ReadOnlySpan<UIModel> objects, RenderObjectAction render)
    {
        // TODO: Use depth to performance optimization.
        // UI rendering disables depth buffer.
        // Render in the order of UIElement tree so that child elements are rendered in front.

        var rootElement = _rootElement;
        var screenSize = Screen.ClientSize;
        Matrix4.OrthographicProjection(0, (float)screenSize.X, 0, (float)screenSize.Y, -1f, 1f, out var proj);
        var GLToWebGpu = new Matrix4(
            new Vector4(1, 0, 0, 0),
            new Vector4(0, 1, 0, 0),
            new Vector4(0, 0, 0.5f, 0),
            new Vector4(0, 0, 0.5f, 1));
        var uiProjection = GLToWebGpu * proj;

        if(rootElement != null) {
            RenderElementRecursively(pass, rootElement, render, screenSize, uiProjection);
        }

        static void RenderElementRecursively(in RenderPass pass, UIElement element, RenderObjectAction render, in Vector2u screenSize, in Matrix4 uiProjection)
        {
            var model = element.Model;
            Debug.Assert(model != null);
            element.UpdateMaterial(screenSize, uiProjection);
            render(in pass, model);
            foreach(var child in element.Children) {
                RenderElementRecursively(pass, child, render, screenSize, uiProjection);
            }
        }
    }

    protected override OwnRenderPass CreateRenderPass(in OperationContext context)
    {
        return context.CreateSurfaceRenderPass(colorClear: null, depthStencil: null);
    }

    private static Own<PipelineLayout> CreatePipelineLayout(
        Screen screen,
        out Own<BindGroupLayout> layout0,
        out Own<BindGroupLayout> layout1,
        out Own<BindGroupLayout> layout2)
    {
        return PipelineLayout.Create(screen, new()
        {
            BindGroupLayouts = new[]
            {
                // group 0
                BindGroupLayout.Create(screen, new()
                {
                    Entries = new[]
                    {
                        BindGroupLayoutEntry.Buffer(0, ShaderStages.Vertex | ShaderStages.Fragment, new BufferBindingData { Type = BufferBindingType.Uniform } ),
                        BindGroupLayoutEntry.Buffer(1, ShaderStages.Vertex | ShaderStages.Fragment, new BufferBindingData { Type = BufferBindingType.Uniform } ),
                    },
                }).AsValue(out layout0),

                // group 1
                BindGroupLayout.Create(screen, new()
                {
                    Entries = new[]
                    {
                        BindGroupLayoutEntry.Texture(0, ShaderStages.Vertex | ShaderStages.Fragment, new TextureBindingData
                        {
                            ViewDimension = TextureViewDimension.D2,
                            Multisampled = false,
                            SampleType = TextureSampleType.FloatNotFilterable,
                        }),
                        BindGroupLayoutEntry.Sampler(1, ShaderStages.Vertex | ShaderStages.Fragment, SamplerBindingType.NonFiltering),
                    },
                }).AsValue(out layout1),

                // group 2
                BindGroupLayout.Create(screen, new()
                {
                    Entries = new[]
                    {
                        BindGroupLayoutEntry.Buffer(0, ShaderStages.Vertex | ShaderStages.Fragment, new BufferBindingData { Type = BufferBindingType.Uniform } ),
                    },
                }).AsValue(out layout2),
            },
        });
    }
}
