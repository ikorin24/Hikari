#nullable enable
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
    private FrozenDictionary<PassKind, SortedList<int, List<RenderData>>> _passKinds;
    private bool _isRenderPassSet;

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
        _passKinds = new Dictionary<PassKind, SortedList<int, List<RenderData>>>(0).ToFrozenDictionary();
    }

    public void SetDefaultRenderPass()
    {
        SetDefaultRenderPass(_screen.Closed);
    }

    public void SetDefaultRenderPass<_>(Event<_> resourceLifetime)
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
    }

    public void SetRenderPass(ImmutableArray<RenderPassDefinition> passDefs)
    {
        if(passDefs.IsDefault) {
            ThrowHelper.ThrowArgument(nameof(passDefs));
        }
        _screen.MainThread.ThrowIfNotMatched();
        if(_isRenderPassSet) {
            throw new InvalidOperationException("Already RenderPasses are defined");
        }
        var dic = new Dictionary<PassKind, SortedList<int, List<RenderData>>>();
        foreach(var passDef in passDefs) {
            dic.TryAdd(passDef.Kind, []);
        }
        _passDefs = passDefs;
        _passKinds = dic.ToFrozenDictionary();
        _isRenderPassSet = true;
    }

    internal void Add(Renderer renderer)
    {
        Debug.Assert(_screen.MainThread.IsCurrentThread);
        foreach(var (material, submesh) in renderer.GetSubrenderers()) {
            var passes = material.Shader.ShaderPasses;
            for(int passIndex = 0; passIndex < passes.Length; passIndex++) {
                var pass = passes[passIndex];
                if(_passKinds.TryGetValue(pass.PassKind, out var sortedList)) {
                    var passData = new RenderData(renderer, material, submesh, pass, passIndex);
                    var order = passData.SortOrderInPass;
                    if(sortedList.TryGetValue(order, out var list)) {
                        list.Add(passData);
                    }
                    else {
                        list = [passData];
                        sortedList.Add(order, list);
                    }
                }
            }
        }
    }

    internal void RemoveRenderer(Renderer renderer)
    {
        Debug.Assert(Screen.MainThread.IsCurrentThread);
        foreach(var material in renderer.Materials.AsSpan()) {
            var passes = material.Shader.ShaderPasses.AsSpan();
            for(int passIndex = 0; passIndex < passes.Length; passIndex++) {
                var pass = passes[passIndex];

                if(_passKinds.TryGetValue(pass.PassKind, out var sortedList)) {
                    if(sortedList.TryGetValue(pass.SortOrderInPass, out var list)) {

                        int indexInList = -1;
                        {
                            var span = list.AsSpan();
                            for(int i = 0; i < span.Length; i++) {
                                if(span[i].Renderer == renderer && span[i].PassIndex == passIndex) {
                                    indexInList = i;
                                }
                            }
                        }
                        if(indexInList > 0) {
                            list.RemoveAt(indexInList);
                        }

                    }
                }
            }
        }
    }

    internal void Execute()
    {
        Debug.Assert(_screen.MainThread.IsCurrentThread);

        var passDefs = _passDefs.AsSpan();
        if(passDefs.Length == 0) {
            passDefs = new ReadOnlySpan<RenderPassDefinition>(in EmptyPass);
            Debug.WriteLine($"No RenderPass is set. Set RenderPass to '{nameof(Hikari.Screen)}.{nameof(Hikari.Screen.RenderScheduler)}'");
        }
        var screen = _screen;
        foreach(var passDef in passDefs) {
            using var renderPassOwn = passDef.Factory(screen, passDef.UserData);
            var renderPass = renderPassOwn.AsValue();
            if(_passKinds.TryGetValue(passDef.Kind, out var sortedList)) {
                foreach(var list in sortedList.Values) {
                    foreach(var data in list.AsSpan()) {
                        data.Render(renderPass);
                    }
                }
            }
        }
    }

    private sealed class RenderData
    {
        private readonly Renderer _renderer;
        private readonly IMaterial _material;
        private readonly Mesh _mesh;
        private readonly SubmeshData _submeshData;
        private readonly RenderPipeline _pipeline;
        private readonly RenderPassAction _onRenderPass;
        private readonly int _passIndex;
        private readonly int _sortOrderInPass;

        public Renderer Renderer => _renderer;
        public int PassIndex => _passIndex;
        public int SortOrderInPass => _sortOrderInPass;

        public RenderData(Renderer renderer, IMaterial material, SubmeshData submeshData, ShaderPassData shaderPass, int passIndex)
        {
            _renderer = renderer;
            _material = material;
            _mesh = renderer.Mesh;
            _submeshData = submeshData;
            _pipeline = shaderPass.Pipeline;
            _onRenderPass = shaderPass.OnRenderPass;
            _passIndex = passIndex;
            _sortOrderInPass = shaderPass.SortOrderInPass;
        }

        public void Render(in RenderPass renderPass)
        {
            var state = new RenderPassState
            {
                RenderPass = renderPass,
                Pipeline = _pipeline,
                Material = _material,
                Mesh = _mesh,
                Submesh = _submeshData,
                PassIndex = _passIndex,
                Renderer = _renderer,
            };
            _onRenderPass.Invoke(in state);
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
