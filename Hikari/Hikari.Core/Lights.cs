#nullable enable

namespace Hikari;

public sealed partial class Lights
{
    private readonly Screen _screen;
    private readonly DirectionalLight _dirLight;
    private BufferCached<BufferData> _buffer;
    private readonly Own<BindGroupLayout> _bindGroupLayout;
    private readonly Own<BindGroup> _bindGroup;

    public Screen Screen => _screen;

    public DirectionalLight DirectionalLight => _dirLight;

    public float AmbientStrength
    {
        get => _buffer.Data.AmbientStrength;
        set
        {
            _buffer.WriteData(new BufferData
            {
                AmbientStrength = value,
            });
        }
    }

    public BindGroupLayout DataBindGroupLayout => _bindGroupLayout.AsValue();
    public BindGroup DataBindGroup => _bindGroup.AsValue();

    internal Lights(Screen screen)
    {
        _screen = screen;
        _dirLight = new DirectionalLight(screen);
        _buffer = new(screen, new BufferData
        {
            AmbientStrength = 0.2f,
        }, BufferUsages.Storage | BufferUsages.CopyDst);
        _bindGroupLayout = BindGroupLayout.Create(screen, new BindGroupLayoutDescriptor
        {
            Entries = new BindGroupLayoutEntry[]
            {
                BindGroupLayoutEntry.Buffer(0, ShaderStages.Vertex | ShaderStages.Fragment | ShaderStages.Compute, new()
                {
                    Type = BufferBindingType.StorageReadOnly,
                }),
                BindGroupLayoutEntry.Buffer(1, ShaderStages.Vertex | ShaderStages.Fragment | ShaderStages.Compute, new()
                {
                    Type = BufferBindingType.StorageReadOnly,
                }),
            },
        });
        _bindGroup = BindGroup.Create(screen, new BindGroupDescriptor
        {
            Layout = _bindGroupLayout.AsValue(),
            Entries = new BindGroupEntry[]
            {
                BindGroupEntry.Buffer(0, _dirLight.DataBuffer),
                BindGroupEntry.Buffer(1, _buffer.AsBuffer()),
            },
        });
    }

    internal void DisposeInternal()
    {
        _dirLight.DisposeInternal();
        _buffer.Dispose();
        _bindGroupLayout.Dispose();
        _bindGroup.Dispose();
    }

    private partial record struct BufferData
    {
        public float AmbientStrength;
    }
}
