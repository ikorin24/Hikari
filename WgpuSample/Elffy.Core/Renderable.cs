#nullable enable

namespace Elffy;

public abstract class Renderable<TLayer, TVertex, TShader, TMaterial>
    : Positionable<TLayer, TVertex, TShader, TMaterial>
    where TLayer : ObjectLayer<TLayer, TVertex, TShader, TMaterial>
    where TVertex : unmanaged, IVertex
    where TShader : Shader<TShader, TMaterial>
    where TMaterial : Material<TMaterial, TShader>
{
    private readonly MaybeOwn<TMaterial> _material;
    private readonly MaybeOwn<Mesh<TVertex>> _mesh;

    public TMaterial Material => _material.AsValue();
    public Mesh<TVertex> Mesh => _mesh.AsValue();

    public bool IsOwnMaterial => _material.IsOwn(out _);
    public bool IsOwnMesh => _mesh.IsOwn(out _);

    protected Renderable(TLayer layer, MaybeOwn<Mesh<TVertex>> mesh, MaybeOwn<TMaterial> material) : base(layer)
    {
        material.ThrowArgumentExceptionIfNone();
        mesh.ThrowArgumentExceptionIfNone();
        _material = material;
        _mesh = mesh;
    }

    internal void InvokeRender(RenderPass pass) => Render(pass);

    protected abstract void Render(RenderPass pass);

    internal override void OnDead()
    {
        base.OnDead();
        _material.Dispose();
        _mesh.Dispose();
    }
}
