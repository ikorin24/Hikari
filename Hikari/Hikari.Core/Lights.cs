#nullable enable

namespace Hikari;

public sealed class Lights
{
    private readonly Screen _screen;
    private readonly DirectionalLight _dirLight;
    private readonly Own<Buffer> _buffer;
    private BufferData _bufferData;
    private readonly Own<BindGroupLayout> _bindGroupLayout;
    private readonly Own<BindGroup> _bindGroup;

    public Screen Screen => _screen;

    public DirectionalLight DirectionalLight => _dirLight;

    public float AmbientStrength
    {
        get => _bufferData.AmbientStrength;
        set
        {
            var newData = new BufferData
            {
                AmbientStrength = value,
            };
            _buffer.AsValue().WriteData(0, in newData);
            _bufferData = newData;
        }
    }

    public BindGroupLayout DataBindGroupLayout => _bindGroupLayout.AsValue();
    public BindGroup DataBindGroup => _bindGroup.AsValue();

    internal Lights(Screen screen)
    {
        _screen = screen;
        _dirLight = new DirectionalLight(screen);
        _bufferData = new BufferData
        {
            AmbientStrength = 0.2f,
        };
        _buffer = Buffer.CreateInitData(screen, _bufferData, BufferUsages.Storage);
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
                BindGroupEntry.Buffer(1, _buffer.AsValue()),
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

    //[StructLayout(LayoutKind.Sequential, Pack = )]
    private record struct BufferData
    {
        public float AmbientStrength;
    }
}
