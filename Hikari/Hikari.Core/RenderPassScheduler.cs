#nullable enable
using Hikari.Threading;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Hikari;

public sealed class RenderPassScheduler
{
    private readonly Screen _screen;
    private ImmutableArray<RenderPassDefinition> _passDefs;
    private FrozenDictionary<PassKind, List<Data>> _passDataDic;
    private FastSpinLock _lock;

    private static readonly RenderPassDefinition EmptyPass = new()
    {
        Kind = PassKind.Surface,
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
        _passDataDic = new Dictionary<PassKind, List<Data>>(0).ToFrozenDictionary();
    }

    public void SetRenderPass(ImmutableArray<RenderPassDefinition> passDefs)
    {
        if(passDefs.IsDefault) {
            ThrowHelper.ThrowArgument(nameof(passDefs));
        }
        using(_lock.Scope()) {
            _passDefs = passDefs;
            var dic = new Dictionary<PassKind, List<Data>>();
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
                if(_passDataDic.TryGetValue(passes[i].PassKind, out var list)) {
                    var data = new Data
                    {
                        OnRenderPass = passes[i].OnRenderPass,
                        Material = material,
                        Mesh = mesh,
                        Submesh = submesh,
                        Pipeline = passes[i].Pipeline,
                        PassIndex = i,
                    };
                    list.Add(data);     // TODO: lazy add
                    //                                        // TODO: sort
                }
            }
        }
    }

    internal void RemoveRenderer()      // TODO:
    {
        throw new NotImplementedException();
    }

    internal void Execute()
    {
        ReadOnlySpan<RenderPassDefinition> passDefs;
        using(_lock.Scope()) {
            passDefs = _passDefs.AsSpan();
        }
        if(passDefs.Length == 0) {
            passDefs = new ReadOnlySpan<RenderPassDefinition>(in EmptyPass);
        }

        var screen = _screen;
        foreach(var passDef in passDefs) {
            using var renderPassOwn = passDef.Factory(screen, passDef.UserData);
            var renderPass = renderPassOwn.AsValue();

            if(_passDataDic.TryGetValue(passDef.Kind, out var list)) {
                foreach(var data in list.AsSpan()) {
                    data.Render(renderPass);
                }
            }
        }
    }


    private readonly record struct Data
    {
        public required RenderPassAction OnRenderPass { get; init; }
        public required Material Material { get; init; }
        public required Mesh Mesh { get; init; }
        public required SubmeshData Submesh { get; init; }
        public required RenderPipeline Pipeline { get; init; }
        public required int PassIndex { get; init; }

        public void Render(in RenderPass renderPass)
        {
            OnRenderPass.Invoke(renderPass, Pipeline, Material, Mesh, Submesh, PassIndex);
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
