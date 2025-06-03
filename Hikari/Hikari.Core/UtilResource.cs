#nullable enable

namespace Hikari;

public sealed class UtilResource
{
    private readonly Screen _screen;
    private readonly Own<Sampler> _linearSampler;

    public Screen Screen => _screen;
    public Sampler LinearSampler => _linearSampler.AsValue();

    internal UtilResource(Screen screen)
    {
        _screen = screen;
        _linearSampler = Sampler.Create(screen, new()
        {
            AddressModeU = AddressMode.ClampToEdge,
            AddressModeV = AddressMode.ClampToEdge,
            AddressModeW = AddressMode.ClampToEdge,
            MagFilter = FilterMode.Linear,
            MinFilter = FilterMode.Linear,
            MipmapFilter = FilterMode.Linear,
        });
    }

    internal void DisposeInternal()
    {
        _linearSampler.Dispose();
    }
}
