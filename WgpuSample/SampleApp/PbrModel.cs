#nullable enable
using System;
using V = Elffy.Vertex;

namespace Elffy;

public sealed class PbrModel : Renderable<PbrLayer, V, PbrShader, PbrMaterial>
{
    private Own<Buffer> _tangent;

    public PbrModel(
        PbrLayer layer,
        Own<Mesh<V>> mesh,
        Own<Sampler> sampler,
        Own<Texture> albedo,
        Own<Texture> metallicRoughness,
        Own<Texture> normal)
        : base(layer, mesh, PbrMaterial.Create(layer.Shader, sampler, albedo, metallicRoughness, normal))
    {
        //V.Fields.Span.Contains(VertexField)
        if(V.Fields.Contains(VertexFieldSemantics.Tangent) == false) {

        }
    }

    private static Vector3 CalcTangent(in Vector3 pos0, in Vector3 pos1, in Vector3 pos2, in Vector2 uv0, in Vector2 uv1, in Vector2 uv2)
    {
        var deltaUV1 = uv1 - uv0;
        var deltaUV2 = uv2 - uv0;
        var deltaPos1 = pos1 - pos0;
        var deltaPos2 = pos2 - pos0;
        var d = 1f / (deltaUV1.X * deltaUV2.Y - deltaUV1.Y * deltaUV2.X);
        var tangent = d * (deltaUV2.Y * deltaPos1 - deltaUV1.Y * deltaPos2);
#if DEBUG
        var bitangent = d * (deltaUV1.X * deltaPos2 - deltaUV2.X * deltaPos1);
#endif
        return tangent;
    }
}
