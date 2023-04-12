#nullable enable

namespace Elffy;

public sealed class DeferredProcessMaterial : Material<DeferredProcessMaterial, DeferredProcessShader>
{
    private readonly Own<BindGroup> _bindGroup0;

    public BindGroup BindGroup0 => _bindGroup0.AsValue();

    private DeferredProcessMaterial(DeferredProcessShader shader, Own<BindGroup> bindGroup0) : base(shader)
    {
        _bindGroup0 = bindGroup0;
    }

    protected override void Release(bool manualRelease)
    {
        base.Release(manualRelease);
        if(manualRelease) {
            _bindGroup0.Dispose();
        }
    }

    public static Own<DeferredProcessMaterial> Create(DeferredProcessShader shader, GBuffer gBuffer)
    {
        var screen = shader.Screen;
        var sampler = Sampler.NoMipmap(screen, AddressMode.ClampToEdge, FilterMode.Nearest, FilterMode.Nearest);
        var desc = new BindGroupDescriptor
        {
            Layout = shader.BindGroupLayout0,
            Entries = new BindGroupEntry[]
            {
                BindGroupEntry.Sampler(0, sampler.AsValue()),
                BindGroupEntry.TextureView(1, gBuffer.ColorAttachment(0).View),
                BindGroupEntry.TextureView(2, gBuffer.ColorAttachment(1).View),
                BindGroupEntry.TextureView(3, gBuffer.ColorAttachment(2).View),
                BindGroupEntry.TextureView(4, gBuffer.ColorAttachment(3).View),
            },
        };
        var bindGroup0 = BindGroup.Create(screen, desc);
        var material = new DeferredProcessMaterial(shader, bindGroup0);
        return CreateOwn(material);
    }
}
