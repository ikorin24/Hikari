#nullable enable
using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Hikari.UI;

internal sealed class UIModel : FrameObject<UIModel, VertexSlim, UIShader, UIMaterial>
{
    private readonly UIElement _element;

    internal UIElement Element => _element;

    internal UIModel(UIElement element, UIShader shader)
        : base(
            GetMesh(shader.Screen),
            shader.CreateMaterial())
    {
        _element = element;
    }

    protected override void Render(in RenderPass renderPass, ShaderPass shaderPass)
    {
        switch(shaderPass.Index) {
            case 0: {
                var screen = Screen;
                var screenSize = screen.ClientSize;
                var scaleFactor = screen.ScaleFactor;
                var uiProjection = Matrix4.ReversedZ.OrthographicProjection(0, (float)screenSize.X, 0, (float)screenSize.Y, 0, 1f);
                _element.UpdateMaterial(screenSize, scaleFactor, uiProjection, 0);

                var mesh = Mesh;
                var material = Material;
                renderPass.SetVertexBuffer(0, mesh.VertexBuffer);
                renderPass.SetIndexBuffer(mesh.IndexBuffer, mesh.IndexFormat);
                renderPass.SetBindGroups(material.Passes[0].BindGroups);
                renderPass.DrawIndexed(mesh.IndexCount);
                return;
            }
            default: {
                Debug.Fail("unreachable");
                return;
            }
        }
    }

    private static Mesh<VertexSlim> GetMesh(Screen screen)
    {
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
            var mesh = Hikari.Mesh.Create(screen, vertices, indices);
            screen.Closed.Subscribe(static screen =>
            {
                if(_cache.TryRemove(screen, out var cache)) {
                    cache.Dispose();
                }
            });
            return new MeshCache(mesh);
        });
        return cache.Mesh;
    }

    private static readonly ConcurrentDictionary<Screen, MeshCache> _cache = new();

    private sealed class MeshCache : IDisposable
    {
        private Own<Mesh<VertexSlim>> _mesh;

        public Mesh<VertexSlim> Mesh => _mesh.AsValue();

        public MeshCache(Own<Mesh<VertexSlim>> mesh)
        {
            _mesh = mesh;
        }

        public void Dispose()
        {
            _mesh.Dispose();
            _mesh = Own<Mesh<VertexSlim>>.None;
        }
    }
}
