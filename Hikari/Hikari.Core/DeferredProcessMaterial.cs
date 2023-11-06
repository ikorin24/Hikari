#nullable enable
using Hikari.Internal;
using System;

namespace Hikari;

public sealed class DeferredProcessMaterial : Material<DeferredProcessMaterial, DeferredProcessShader>
{
    private MaterialPassData _pass0;
    private DisposableBag? _disposable;

    public override ReadOnlySpan<MaterialPassData> Passes => new ReadOnlySpan<MaterialPassData>(in _pass0);

    private DeferredProcessMaterial(DeferredProcessShader shader, IGBufferProvider gBufferProvider) : base(shader)
    {
        ArgumentNullException.ThrowIfNull(gBufferProvider);
        _pass0 = CreatePass0BindGroups(Shader, gBufferProvider.GetCurrentGBuffer(), out var disposable);
        _disposable?.Dispose();
        _disposable = disposable;

        gBufferProvider.GBufferChanged.Subscribe(gBufferProvider =>
        {
            _pass0 = CreatePass0BindGroups(Shader, gBufferProvider.GetCurrentGBuffer(), out var disposable);
            _disposable?.Dispose();
            _disposable = disposable;
        }).DisposeOn(Disposed);
    }

    internal static Own<DeferredProcessMaterial> Create(DeferredProcessShader shader, IGBufferProvider gBufferProvider)
    {
        var material = new DeferredProcessMaterial(shader, gBufferProvider);
        return CreateOwn(material);
    }

    private static MaterialPassData CreatePass0BindGroups(DeferredProcessShader shader, GBuffer gBuffer, out DisposableBag disposable)
    {
        var screen = shader.Screen;
        var directionalLight = screen.Lights.DirectionalLight;
        disposable = new DisposableBag();
        return new MaterialPassData(0, new[]
        {
            new BindGroupData
            {
                Index = 0,
                BindGroup = BindGroup.Create(screen, new()
                {
                    Layout = shader.Passes[0].Layout.BindGroupLayouts[0],
                    Entries = new BindGroupEntry[]
                    {
                        BindGroupEntry.Sampler(0, Sampler.Create(screen, new()
                        {
                            AddressModeU = AddressMode.ClampToEdge,
                            AddressModeV = AddressMode.ClampToEdge,
                            AddressModeW = AddressMode.ClampToEdge,
                            MagFilter = FilterMode.Nearest,
                            MinFilter = FilterMode.Nearest,
                            MipmapFilter = FilterMode.Nearest,
                        }).AddTo(disposable)),
                        BindGroupEntry.TextureView(1, gBuffer[0].View),
                        BindGroupEntry.TextureView(2, gBuffer[1].View),
                        BindGroupEntry.TextureView(3, gBuffer[2].View),
                        BindGroupEntry.TextureView(4, gBuffer[3].View),
                    },
                }).AddTo(disposable)
            },
            new BindGroupData
            {
                Index = 1,
                BindGroup = screen.Camera.CameraDataBindGroup,
            },
            new BindGroupData
            {
                Index = 2,
                BindGroup = screen.Lights.DataBindGroup,
            },
            new BindGroupData
            {
                Index = 3,
                BindGroup = BindGroup.Create(screen, new()
                {
                    Layout = shader.Passes[0].Layout.BindGroupLayouts[3],
                    Entries = new[]
                    {
                        BindGroupEntry.TextureView(0, directionalLight.ShadowMap.View),
                        BindGroupEntry.Sampler(1, Sampler.Create(screen, new()
                        {
                            AddressModeU = AddressMode.ClampToEdge,
                            AddressModeV = AddressMode.ClampToEdge,
                            AddressModeW = AddressMode.ClampToEdge,
                            MagFilter = FilterMode.Nearest,
                            MinFilter = FilterMode.Nearest,
                            MipmapFilter = FilterMode.Nearest,
                            Compare = CompareFunction.Greater,
                        }).AddTo(disposable)),
                        BindGroupEntry.Buffer(2, directionalLight.LightMatricesBuffer),
                        BindGroupEntry.Buffer(3, directionalLight.CascadeFarsBuffer),
                    },
                }).AddTo(disposable)
            },
        });
    }
}
