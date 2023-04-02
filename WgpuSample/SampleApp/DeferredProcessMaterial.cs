#nullable enable

namespace Elffy;

public sealed class DeferredProcessMaterial : Material<DeferredProcessMaterial, DeferredProcessShader>
{
    private DeferredProcessMaterial(DeferredProcessShader shader, Own<BindGroup>[] bindGroupOwns) : base(shader, bindGroupOwns)
    {
    }

    public static Own<DeferredProcessMaterial> Create(DeferredProcessShader shader, GBuffer gBuffer)
    {
        var screen = shader.Screen;
        var sampler = Sampler.NoMipmap(screen, AddressMode.ClampToEdge, FilterMode.Nearest, FilterMode.Nearest);
        var desc = new BindGroupDescriptor
        {
            Layout = shader.GetBindGroupLayout(0),
            Entries = new BindGroupEntry[]
            {
                BindGroupEntry.Sampler(0, sampler.AsValue()),
                BindGroupEntry.TextureView(1, gBuffer.ColorAttachment(0).View),
                BindGroupEntry.TextureView(2, gBuffer.ColorAttachment(1).View),
                BindGroupEntry.TextureView(3, gBuffer.ColorAttachment(2).View),
                BindGroupEntry.TextureView(4, gBuffer.ColorAttachment(3).View),
            },
        };
        var bg = BindGroup.Create(screen, desc);
        var material = new DeferredProcessMaterial(shader, new[] { bg });
        return CreateOwn(material);
    }
}
