#nullable enable
using Hikari.Threading;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Hikari;

public sealed class RenderPassScheduler
{
    private readonly Screen _screen;
    private ImmutableArray<RenderPassDefinition> _passDefs;
    private FrozenDictionary<PassKind, Dictionary<Renderer, List<RenderPassData>>> _passDataDic;
    private FastSpinLock _lock;

    private static readonly RenderPassDefinition EmptyPass = new()
    {
        Kind = PassKind.Forward,
        Factory = static (screen, _) => RenderPass.Create(screen,
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
            }),
    };

    public Screen Screen => _screen;

    internal RenderPassScheduler(Screen screen)
    {
        _screen = screen;
        _passDefs = [];
        _passDataDic = new Dictionary<PassKind, Dictionary<Renderer, List<RenderPassData>>>(0).ToFrozenDictionary();
    }

    public DefaultGBufferProvider SetDefault()
    {
        return SetDefault(_screen.Closed);
    }

    public DefaultGBufferProvider SetDefault<_>(Event<_> resourceLifetime)
    {
        var screen = _screen;
        var gBuffer = DefaultGBufferProvider.CreateScreenSize(screen).DisposeOn(resourceLifetime);
        SetRenderPass([
            ..screen.Lights.DirectionalLight.ShadowMapPassDefinitions,
            new RenderPassDefinition
            {
                Kind = PassKind.Deferred,
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
                Kind = PassKind.Forward,
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
        var shader = DeferredProcessShader.Create(screen, gBuffer).DisposeOn(resourceLifetime);
        var material = DeferredProcessMaterial.Create(shader, gBuffer);
        const float Z = 0;
        ReadOnlySpan<VertexSlim> vertices =
        [
            new(new(-1, -1, Z), new(0, 1)),
            new(new(1, -1, Z), new(1, 1)),
            new(new(1, 1, Z), new(1, 0)),
            new(new(-1, 1, Z), new(0, 0)),
        ];
        ReadOnlySpan<ushort> indices = [0, 1, 2, 2, 3, 0];
        var mesh = Mesh.Create<VertexSlim, ushort>(screen, vertices, indices);
        var obj = new FrameObject(mesh.AsValue(), material.AsValue())
        {
            Name = "Deferred Plane",
        };
        resourceLifetime.Subscribe(_ => obj.Terminate());
        mesh.DisposeOn(obj.Dead);
        material.DisposeOn(obj.Dead);
        return gBuffer;
    }

    public void SetRenderPass(ImmutableArray<RenderPassDefinition> passDefs)
    {
        if(passDefs.IsDefault) {
            ThrowHelper.ThrowArgument(nameof(passDefs));
        }
        using(_lock.Scope()) {
            _passDefs = passDefs;
            var dic = new Dictionary<PassKind, Dictionary<Renderer, List<RenderPassData>>>();
            foreach(var passDef in passDefs) {
                dic.TryAdd(passDef.Kind, new());
            }
            _passDataDic = dic.ToFrozenDictionary();
        }
    }

    internal void Add(Renderer renderer)
    {
        foreach(var (material, mesh, submesh) in renderer.GetSubrenderers()) {
            var passes = material.Shader.ShaderPasses;
            for(int i = 0; i < passes.Length; i++) {
                if(_passDataDic.TryGetValue(passes[i].PassKind, out var renderers)) {
                    var data = new RenderPassData
                    {
                        OnRenderPass = passes[i].OnRenderPass,
                        Material = material,
                        Mesh = mesh,
                        Submesh = submesh,
                        Pipeline = passes[i].Pipeline,
                        PassIndex = i,
                    };

                    // TODO: lazy add
                    if(renderers.TryGetValue(renderer, out var dataList)) {
                        dataList.Add(data);
                    }
                    else {
                        renderers.Add(renderer, [data]);
                    }
                    // TODO: need sort?
                }
            }
        }
    }

    internal void RemoveRenderer(Renderer renderer)
    {
        foreach(var (material, mesh, submesh) in renderer.GetSubrenderers()) {
            var passes = material.Shader.ShaderPasses;

            for(int i = 0; i < passes.Length; i++) {
                if(_passDataDic.TryGetValue(passes[i].PassKind, out var renderers)) {
                    // TODO: lazy remove
                    renderers.Remove(renderer);
                }
            }
        }
    }

    internal void Execute()
    {
        ReadOnlySpan<RenderPassDefinition> passDefs;
        using(_lock.Scope()) {
            passDefs = _passDefs.AsSpan();
        }
        if(passDefs.Length == 0) {
            passDefs = new ReadOnlySpan<RenderPassDefinition>(in EmptyPass);
            Debug.WriteLine($"No RenderPass is set. Set RenderPass to '{nameof(Hikari.Screen)}.{nameof(Hikari.Screen.RenderScheduler)}'");
        }

        var screen = _screen;
        foreach(var passDef in passDefs) {
            using var renderPassOwn = passDef.Factory(screen, passDef.UserData);
            var renderPass = renderPassOwn.AsValue();
            if(_passDataDic.TryGetValue(passDef.Kind, out var renderers)) {
                foreach(var (renderer, dataList) in renderers) {
                    if(renderer.IsVisibleInHierarchy) {
                        foreach(var data in dataList.AsSpan()) {
                            data.Render(renderPass, renderer);
                        }
                    }
                }
            }
        }
    }

    internal readonly record struct RenderPassData
    {
        public required RenderPassAction OnRenderPass { get; init; }
        public required IMaterial Material { get; init; }
        public required Mesh Mesh { get; init; }
        public required SubmeshData Submesh { get; init; }
        public required RenderPipeline Pipeline { get; init; }
        public required int PassIndex { get; init; }

        public void Render(in RenderPass renderPass, Renderer renderer)
        {
            var state = new RenderPassState
            {
                RenderPass = renderPass,
                Pipeline = Pipeline,
                Material = Material,
                Mesh = Mesh,
                Submesh = Submesh,
                PassIndex = PassIndex,
                Renderer = renderer,
            };
            OnRenderPass.Invoke(in state);
        }
    }
}

public readonly record struct RenderPassDefinition
{
    public required PassKind Kind { get; init; }
    public required RenderPassFunc<object?> Factory { get; init; }
    public object? UserData { get; init; }

    [SetsRequiredMembers]
    public RenderPassDefinition(PassKind kind, RenderPassFunc<object?> factory)
    {
        Kind = kind;
        Factory = factory;
        UserData = null;
    }

    [SetsRequiredMembers]
    public RenderPassDefinition(PassKind kind, object? userData, RenderPassFunc<object?> factory)
    {
        Kind = kind;
        Factory = factory;
        UserData = userData;
    }
}
