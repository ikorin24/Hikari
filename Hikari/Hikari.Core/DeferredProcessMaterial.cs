#nullable enable
using Hikari.Internal;
using System;
using System.Collections.Immutable;

namespace Hikari;

public sealed class DeferredProcessMaterial : Material
{
    private ImmutableArray<BindGroupData> _pass0BindGroups;
    private DisposableBag? _disposable;

    private DeferredProcessMaterial(Shader shader, IGBufferProvider gBuffer) : base(shader)
    {
        gBuffer.Observe(gBuffer =>
        {
            _pass0BindGroups = CreatePass0BindGroups(shader, gBuffer, out var disposable);
            _disposable?.Dispose();
            _disposable = disposable;
        }).DisposeOn(Disposed);
    }

    public override ReadOnlySpan<BindGroupData> GetBindGroups(int passIndex)
    {
        return passIndex switch
        {
            0 => _pass0BindGroups.AsSpan(),
            _ => throw new ArgumentOutOfRangeException(nameof(passIndex))
        };
    }

    internal static Own<Material> Create(DeferredProcessShader shader, IGBufferProvider gBuffer)
    {
        ArgumentNullException.ThrowIfNull(gBuffer);
        ArgumentNullException.ThrowIfNull(shader);

        var material = new DeferredProcessMaterial(shader, gBuffer);
        return CreateOwn(material).Cast<Material>();
    }

    private static ImmutableArray<BindGroupData> CreatePass0BindGroups(Shader shader, GBuffer gBuffer, out DisposableBag disposable)
    {
        var screen = shader.Screen;
        var passes = shader.ShaderPasses;
        var directionalLight = screen.Lights.DirectionalLight;
        var gTextures = gBuffer.Textures;
        disposable = new DisposableBag();
        return [
            new BindGroupData
            {
                Index = 0,
                BindGroup = BindGroup.Create(screen, new()
                {
                    Layout = passes[0].Pipeline.Layout.BindGroupLayouts[0],
                    Entries =
                    [
                        BindGroupEntry.Sampler(0, Sampler.Create(screen, new()
                        {
                            AddressModeU = AddressMode.ClampToEdge,
                            AddressModeV = AddressMode.ClampToEdge,
                            AddressModeW = AddressMode.ClampToEdge,
                            MagFilter = FilterMode.Nearest,
                            MinFilter = FilterMode.Nearest,
                            MipmapFilter = FilterMode.Nearest,
                        }).AddTo(disposable)),
                        BindGroupEntry.TextureView(1, gTextures[0].View),
                        BindGroupEntry.TextureView(2, gTextures[1].View),
                        BindGroupEntry.TextureView(3, gTextures[2].View),
                        BindGroupEntry.TextureView(4, gTextures[3].View),
                    ],
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
                    Layout = passes[0].Pipeline.Layout.BindGroupLayouts[3],
                    Entries =
                    [
                        BindGroupEntry.TextureView(0, directionalLight.ShadowMap.View),
                        BindGroupEntry.Buffer(1, directionalLight.LightMatricesBuffer),
                        BindGroupEntry.Buffer(2, directionalLight.CascadeFarsBuffer),
                    ],
                }).AddTo(disposable)
            },
        ];
    }
}
