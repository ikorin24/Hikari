﻿#nullable enable
using System;
using System.Collections.Immutable;

namespace Hikari;

public readonly record struct ShaderPassDescriptor
{
    public required ImmutableArray<byte> Source { get; init; }
    public required PipelineLayoutDescriptor LayoutDescriptor { get; init; }
    public required Func<ShaderModule, PipelineLayout, RenderPipelineDescriptor> PipelineDescriptorFactory { get; init; }
    public required int SortOrderInPass { get; init; }
    public required PassKind PassKind { get; init; }
    public required RenderPassAction OnRenderPass { get; init; }

    internal ShaderPassData CreateShaderPassData<_>(Shader shader, int passIndex, Event<_> lifetimeLimit)
    {
        var screen = shader.Screen;
        var module = ShaderModule.Create(screen, Source.AsSpan()).DisposeOn(lifetimeLimit);
        var layout = PipelineLayout.Create(screen, LayoutDescriptor).DisposeOn(lifetimeLimit);
        return new ShaderPassData
        {
            Index = passIndex,
            PassKind = PassKind,
            SortOrderInPass = SortOrderInPass,
            Pipeline = RenderPipeline.Create(screen, PipelineDescriptorFactory(module, layout)).DisposeOn(lifetimeLimit),
            OnRenderPass = OnRenderPass,
        };
    }
}

public delegate void RenderPassAction(in RenderPassState state);

public readonly ref struct RenderPassState
{
    public required RenderPass RenderPass { get; init; }
    public required RenderPipeline Pipeline { get; init; }
    public required IMaterial Material { get; init; }
    public required Mesh Mesh { get; init; }
    public required SubmeshData Submesh { get; init; }
    public required int PassIndex { get; init; }
    public required Renderer Renderer { get; init; }

    public void DefaultDrawIndexed()
    {
        RenderPass.SetPipeline(Pipeline);
        Material.SetBindGroupsTo(RenderPass, PassIndex, Renderer);
        RenderPass.SetVertexBuffer(0, Mesh.VertexBuffer);
        RenderPass.SetIndexBuffer(Mesh.IndexBuffer, Mesh.IndexFormat);
        RenderPass.DrawIndexed(Submesh.IndexOffset, Submesh.IndexCount, Submesh.VertexOffset, 0, 1);
    }
}
