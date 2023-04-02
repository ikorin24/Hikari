#nullable enable
using V = Elffy.Vertex;

namespace Elffy;

public sealed class PbrModel : Renderable<PbrLayer, V, PbrShader, PbrMaterial>
{
    public PbrModel(
        PbrLayer layer,
        Own<Mesh<V>> mesh,
        Own<Sampler> sampler,
        Own<Texture> albedo,
        Own<Texture> metallicRoughness,
        Own<Texture> normal)
        : base(layer, mesh, PbrMaterial.Create(layer.Shader, sampler, albedo, metallicRoughness, normal))
    {
    }
}
