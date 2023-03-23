#nullable enable

namespace Elffy;

public abstract class Renderable<TLayer, TVertex, TShader, TMaterial>
    : Positionable<TLayer, TVertex, TShader, TMaterial>
    where TLayer : ObjectLayer<TLayer, TVertex, TShader, TMaterial>
    where TVertex : unmanaged, IVertex<TVertex>
    where TShader : Shader<TShader, TMaterial>
    where TMaterial : Material<TMaterial, TShader>
{
    private readonly Own<TMaterial> _material;
    private readonly Own<Mesh> _mesh;

    public TMaterial Material => _material.AsValue();
    public Mesh Mesh => _mesh.AsValue();

    protected Renderable(TLayer layer, Own<Mesh> mesh, Own<TMaterial> material) : base(layer)
    {
        material.ThrowArgumentExceptionIfNone();
        mesh.ThrowArgumentExceptionIfNone();
        _material = material;
        _mesh = mesh;
    }

    internal void Render(RenderPass renderPass)
    {
        var material = Material;
        var bindGroups = material.BindGroups.Span;
        var mesh = _mesh.AsValue();
        for(int i = 0; i < bindGroups.Length; i++) {
            renderPass.SetBindGroup((uint)i, bindGroups[i]);
        }

        renderPass.SetVertexBuffer(0, mesh.VertexBuffer);
        renderPass.SetIndexBuffer(mesh.IndexBuffer, mesh.IndexFormat);
        renderPass.DrawIndexed(0, mesh.IndexCount, 0, 0, 1);
    }

    internal override void OnDead()
    {
        base.OnDead();
        _material.Dispose();
        _mesh.Dispose();
    }
}
