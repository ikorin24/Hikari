#nullable enable
using System;
using V = Elffy.Vertex;

namespace Elffy;

public sealed class PbrModel : Renderable<PbrModel, PbrLayer, V, PbrShader, PbrMaterial>
{
    private readonly BufferSlice<Vector3> _tangent;
    private readonly Own<BindGroup> _shadowBindGroup0;

    public PbrModel(PbrLayer layer, MaybeOwn<Mesh<V>> mesh, Own<PbrMaterial> material)
        : base(layer, mesh, material)
    {
        var m = Mesh;
        if(m.TryGetOptionalTangent(out var tangent) == false) {
            throw new ArgumentException("The mesh does not have Tangent vertex buffer", nameof(mesh));
        }
        _tangent = tangent;

        var screen = Screen;
        var mat = Material;
        var model = mat.ModelUniform;
        _shadowBindGroup0 = BindGroup.Create(screen, new()
        {
            Layout = layer.ShadowBindGroupLayout0,
            Entries = new[]
            {
                BindGroupEntry.Buffer(0, m.VertexBuffer),
                BindGroupEntry.Buffer(1, m.IndexBuffer.AsBufferSlice()),
                BindGroupEntry.Buffer(2, model),
            },
        });

        Dead.Subscribe(x =>
        {
            var self = SafeCast.As<PbrModel>(x);
            self._shadowBindGroup0.Dispose();
        }).AddTo(Subscriptions);
    }

    protected override void Render(in RenderPass pass, PbrMaterial material, Mesh<V> mesh)
    {
        var (bindGroup0, bindGroup1) = material.GetBindGroups();
        material.WriteModelUniform(GetModel());
        pass.SetBindGroup(0, bindGroup0);
        pass.SetBindGroup(1, bindGroup1);
        pass.SetVertexBuffer(0, mesh.VertexBuffer);
        pass.SetVertexBuffer(1, _tangent);
        pass.SetIndexBuffer(mesh.IndexBuffer);
        pass.DrawIndexed(0, mesh.IndexCount, 0, 0, 1);
    }

    internal void RenderShadowMap(in RenderShadowMapContext context, in ComputePass pass)
    {
        pass.SetBindGroup(0, _shadowBindGroup0.AsValue());
        pass.SetBindGroup(1, context.LightDepthBindGroup);
        pass.DispatchWorkgroups(0, 0, 0);
    }
}
