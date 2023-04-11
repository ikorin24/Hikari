#nullable enable

namespace Elffy;

public abstract class Renderable<TLayer, TVertex, TShader, TMaterial>
    : Positionable<TLayer, TVertex, TShader, TMaterial>
    where TLayer : ObjectLayer<TLayer, TVertex, TShader, TMaterial>
    where TVertex : unmanaged, IVertex
    where TShader : Shader<TShader, TMaterial>
    where TMaterial : Material<TMaterial, TShader>
{
    private readonly Own<TMaterial> _material;
    private readonly Own<Mesh<TVertex>> _mesh;

    public TMaterial Material => _material.AsValue();
    public Mesh<TVertex> Mesh => _mesh.AsValue();

    protected Renderable(TLayer layer, Own<Mesh<TVertex>> mesh, Own<TMaterial> material) : base(layer)
    {
        material.ThrowArgumentExceptionIfNone();
        mesh.ThrowArgumentExceptionIfNone();
        _material = material;
        _mesh = mesh;
    }

    internal void InvokeRender(RenderPass renderPass) => Render(renderPass);

    protected abstract void Render(RenderPass renderPass);

    internal override void OnDead()
    {
        base.OnDead();
        _material.Dispose();
        _mesh.Dispose();
    }
}
