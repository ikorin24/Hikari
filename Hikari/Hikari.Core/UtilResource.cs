#nullable enable

namespace Hikari;

public sealed class UtilResource
{
    private readonly Screen _screen;
    private readonly DisposableBag _disposables;
    private readonly Sampler _linearSampler;
    private readonly BindGroupLayout _modelDataBindGroupLayout;

    public Screen Screen => _screen;
    public Sampler LinearSampler => _linearSampler;
    public BindGroupLayout ModelDataBindGroupLayout => _modelDataBindGroupLayout;

    internal UtilResource(Screen screen)
    {
        _screen = screen;
        var disposables = new DisposableBag();
        _linearSampler = Sampler.Create(screen, new()
        {
            AddressModeU = AddressMode.ClampToEdge,
            AddressModeV = AddressMode.ClampToEdge,
            AddressModeW = AddressMode.ClampToEdge,
            MagFilter = FilterMode.Linear,
            MinFilter = FilterMode.Linear,
            MipmapFilter = FilterMode.Linear,
        }).AddTo(disposables);

        _modelDataBindGroupLayout = BindGroupLayout.Create(screen, new()
        {
            Entries =
            [
                BindGroupLayoutEntry.Buffer(0, ShaderStages.Vertex, new() { Type = BufferBindingType.Uniform }),
            ],
        }).AddTo(disposables);
        _disposables = disposables;
    }

    internal void DisposeInternal()
    {
        _disposables.Dispose();
    }
}
