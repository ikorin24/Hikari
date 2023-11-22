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
    private FrozenDictionary<PassKind, List<RenderData>> _passDataDic;
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
        _passDataDic = new Dictionary<PassKind, List<RenderData>>(0).ToFrozenDictionary();
    }

    public void SetRenderPass(ImmutableArray<RenderPassDefinition> passDefs)
    {
        if(passDefs.IsDefault) {
            ThrowHelper.ThrowArgument(nameof(passDefs));
        }
        using(_lock.Scope()) {
            _passDefs = passDefs;
            var dic = new Dictionary<PassKind, List<RenderData>>();
            foreach(var passDef in passDefs) {
                dic.TryAdd(passDef.Kind, new());
            }
            _passDataDic = dic.ToFrozenDictionary();
        }
    }

    internal void Add(FrameObject obj)
    {
        var mat = obj.Material;
        var shader = mat.Shader;
        var passData = shader.MaterialPassData;
        var mesh = obj.Mesh;
        for(int i = 0; i < passData.Length; i++) {
            // It must be copied to local variable
            var index = i;
            foreach(var submesh in mesh.Submeshes) {
                var data = new RenderData
                {
                    Kind = passData[i].PassKind,
                    SortOrder = passData[i].SortOrder,
                    Pipeline = passData[i].Pipeline,
                    BindGroupsProvider = () => mat.GetBindGroups(index),
                    VertexBuffers = mesh.VertexSlots,
                    Indices = mesh.IndexBuffer,
                    IndexFormat = mesh.IndexFormat,
                    TargetSubmesh = submesh,
                    InstanceCount = 1,
                    Object = obj,
                };
                Add(data);
            }
        }
    }

    internal void Add(in RenderData passData)
    {
        if(_passDataDic.TryGetValue(passData.Kind, out var list) == false) {
            return;
        }
        list.Add(passData);     // TODO: lazy add
        // TODO: sort
        list.Sort((a, b) => a.SortOrder - b.SortOrder);
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
                    renderPass.SetPipeline(data.Pipeline);
                    renderPass.SetBindGroups(data.BindGroupsProvider.Invoke());
                    foreach(var (slot, vertices) in data.VertexBuffers) {
                        renderPass.SetVertexBuffer(slot, vertices);
                    }
                    renderPass.SetIndexBuffer(data.Indices, data.IndexFormat);
                    renderPass.DrawIndexed(0, data.TargetSubmesh.IndexCount, data.TargetSubmesh.VertexOffset, 0, data.InstanceCount);
                }
            }
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

internal readonly record struct RenderData
{
    public required PassKind Kind { get; init; }
    public required int SortOrder { get; init; }
    public required RenderPipeline Pipeline { get; init; }
    public required BindGroupsProvider BindGroupsProvider { get; init; }
    public required ImmutableArray<VertexSlotData> VertexBuffers { get; init; }
    public required BufferSlice Indices { get; init; }
    public required IndexFormat IndexFormat { get; init; }
    public required SubmeshData TargetSubmesh { get; init; }
    public required uint InstanceCount { get; init; }

    public required FrameObject Object { get; init; }   // TODO: remove
}

internal delegate ReadOnlySpan<BindGroupData> BindGroupsProvider();
