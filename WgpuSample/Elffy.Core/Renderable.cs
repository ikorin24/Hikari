#nullable enable

namespace Elffy;

public abstract class Renderable<TSelf, TLayer, TVertex, TShader, TMaterial, TScale>
    : FrameObject<TSelf, TLayer, TVertex, TShader, TMaterial>,
      IModel<TScale>
    where TSelf : Renderable<TSelf, TLayer, TVertex, TShader, TMaterial, TScale>
    where TLayer : ObjectLayer<TLayer, TVertex, TShader, TMaterial, TSelf>
    where TVertex : unmanaged, IVertex
    where TShader : Shader<TShader, TMaterial>
    where TMaterial : Material<TMaterial, TShader>
    where TScale : unmanaged
{
    private readonly Own<TMaterial> _material;
    private readonly MaybeOwn<Mesh<TVertex>> _mesh;

    public TMaterial Material => _material.AsValue();
    public Mesh<TVertex> Mesh => _mesh.AsValue();

    public bool IsOwnMesh => _mesh.IsOwn(out _);

    public abstract Vector3 Position { get; set; }
    public abstract Quaternion Rotation { get; set; }
    public abstract TScale Scale { get; set; }

    protected Renderable(TLayer layer, MaybeOwn<Mesh<TVertex>> mesh, Own<TMaterial> material) : base(layer)
    {
        material.ThrowArgumentExceptionIfNone();
        mesh.ThrowArgumentExceptionIfNone();
        _material = material;
        _mesh = mesh;
    }

    private protected sealed override void Render(in RenderPass pass)
    {
        Render(pass, _material.AsValue(), _mesh.AsValue());
    }

    protected abstract void Render(in RenderPass pass, TMaterial material, Mesh<TVertex> mesh);

    internal override void OnDead()
    {
        base.OnDead();
        _material.Dispose();
        _mesh.Dispose();
    }

    public abstract Matrix4 GetModel();
}
