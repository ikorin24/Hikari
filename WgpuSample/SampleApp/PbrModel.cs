#nullable enable
using System;
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
        if(mesh.AsValue().HasOptionalTangent == false) {
            throw new ArgumentException("The mesh does not have 'Tangent' vertex buffer", nameof(mesh));
        }
    }
}
