#nullable enable

namespace Hikari;

public sealed class Lights
{
    private readonly Screen _screen;
    private readonly DirectionalLight _dirLight;
    private readonly Own<BindGroupLayout> _bindGroupLayout;
    private readonly Own<BindGroup> _bindGroup;

    public Screen Screen => _screen;

    public DirectionalLight DirectionalLight => _dirLight;

    public BindGroupLayout DataBindGroupLayout => _bindGroupLayout.AsValue();
    public BindGroup DataBindGroup => _bindGroup.AsValue();

    internal Lights(Screen screen)
    {
        _screen = screen;
        _dirLight = new DirectionalLight(screen);
        _bindGroupLayout = BindGroupLayout.Create(screen, new BindGroupLayoutDescriptor
        {
            Entries = new BindGroupLayoutEntry[]
            {
                BindGroupLayoutEntry.Buffer(
                    0,
                    ShaderStages.Vertex | ShaderStages.Fragment | ShaderStages.Compute,
                    new BufferBindingData
                    {
                        Type = BufferBindingType.StorateReadOnly,
                        HasDynamicOffset = false,
                        MinBindingSize = null,
                    }),
            },
        });
        _bindGroup = BindGroup.Create(screen, new BindGroupDescriptor
        {
            Layout = _bindGroupLayout.AsValue(),
            Entries = new BindGroupEntry[]
            {
                BindGroupEntry.Buffer(0, _dirLight.DataBuffer),
            },
        });
    }

    internal void DisposeInternal()
    {
        _dirLight.DisposeInternal();
        _bindGroupLayout.Dispose();
        _bindGroup.Dispose();
    }
}
