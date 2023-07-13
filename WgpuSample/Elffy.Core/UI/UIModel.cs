#nullable enable
using System;
using System.Collections.Concurrent;

namespace Elffy.UI;

public sealed class UIModel : FrameObject<UIModel, UILayer, VertexSlim, UIShader, UIMaterial>
{
    private readonly UIElement _element;

    internal UIElement Element => _element;

    internal UIModel(UIElement element, UIShader shader)
        : base(
            GetMesh(shader.Screen),
            shader.CreateMaterial())
    {
        _element = element;
        IsFrozen = true;
    }

    protected override void Render(in RenderPass pass, UIMaterial material, Mesh<VertexSlim> mesh)
    {
        pass.SetVertexBuffer(0, mesh.VertexBuffer);
        pass.SetIndexBuffer(mesh.IndexBuffer, mesh.IndexFormat);
        pass.SetBindGroup(0, material.BindGroup0);
        pass.SetBindGroup(1, material.BindGroup1);
        pass.DrawIndexed(mesh.IndexCount);
    }

    private static Mesh<VertexSlim> GetMesh(Screen screen)
    {
        // TODO: remove cache when screen is disposed

        var cache = _cache.GetOrAdd(screen, static screen =>
        {
            ReadOnlySpan<VertexSlim> vertices = stackalloc VertexSlim[4]
            {
                new VertexSlim(0, 1, 0, 0, 0),
                new VertexSlim(0, 0, 0, 0, 1),
                new VertexSlim(1, 0, 0, 1, 1),
                new VertexSlim(1, 1, 0, 1, 0),
            };
            ReadOnlySpan<ushort> indices = stackalloc ushort[6] { 0, 1, 2, 2, 3, 0 };
            var mesh = Elffy.Mesh.Create(screen, vertices, indices);
            return new MeshCache(mesh);
        });
        return cache.Mesh;
    }

    private static readonly ConcurrentDictionary<Screen, MeshCache> _cache = new();

    private sealed class MeshCache
    {
        private readonly Own<Mesh<VertexSlim>> _mesh;

        public Mesh<VertexSlim> Mesh => _mesh.AsValue();

        public MeshCache(Own<Mesh<VertexSlim>> mesh)
        {
            _mesh = mesh;
        }
    }
}
