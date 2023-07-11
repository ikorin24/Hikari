#nullable enable

using System;

namespace Elffy;

public abstract class Renderable<TSelf, TLayer, TVertex, TShader, TMaterial>
    : FrameObject<TSelf, TLayer, TVertex, TShader, TMaterial>
    where TSelf : Renderable<TSelf, TLayer, TVertex, TShader, TMaterial>
    where TLayer : ObjectLayer<TLayer, TVertex, TShader, TMaterial, TSelf>
    where TVertex : unmanaged, IVertex
    where TShader : Shader<TShader, TMaterial>
    where TMaterial : Material<TMaterial, TShader>
{
    private readonly Own<TMaterial> _material;
    private readonly MaybeOwn<Mesh<TVertex>> _mesh;

    public TShader Shader => _material.AsValue().Shader;
    public TMaterial Material => _material.AsValue();
    public Mesh<TVertex> Mesh => _mesh.AsValue();

    public bool IsOwnMesh => _mesh.IsOwn(out _);

    protected Renderable(
        MaybeOwn<Mesh<TVertex>> mesh, Own<TMaterial> material)
        : base((TLayer)material.AsValue().Shader.Operation)    // TODO:
    {
        material.ThrowArgumentExceptionIfNone();
        mesh.ThrowArgumentExceptionIfNone();
        _material = material;
        _mesh = mesh;
    }

    private protected sealed override void Render(in RenderPass pass)
    {
        pass.SetPipeline(Shader.Pipeline);
        Render(pass, _material.AsValue(), _mesh.AsValue());
    }

    protected abstract void Render(in RenderPass pass, TMaterial material, Mesh<TVertex> mesh);

    internal override void OnDead()
    {
        base.OnDead();
        _material.Dispose();
        _mesh.Dispose();
    }
}
