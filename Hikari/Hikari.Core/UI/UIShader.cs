#nullable enable
using Hikari.Internal;
using System;
using System.Collections.Immutable;

namespace Hikari.UI;

internal abstract class UIShader : Shader
{
    private static readonly Lazy<byte[]> _defaultShaderSource = new(() =>
    {
        var fs_main = """
            fn calc_back_color(
                f_pos: vec2<f32>,
                pos: vec2<f32>,
                size: vec2<f32>,
            ) -> vec4<f32> {
                let texel_color = get_texel_color(f_pos, TEXT_HALIGN_CENTER, TEXT_VALIGN_CENTER, pos, size);
                let bg_color = calc_background_brush_color(f_pos, pos, size);
                return blend(texel_color, bg_color);
            }

            @fragment fn fs_main(
                f: V2F,
            ) -> @location(0) vec4<f32> {
                return gamma22(ui_color_shared_algorithm(f));
            }
            """u8;

        using var sb = Utf8StringBuilder.FromLines(
            UIShaderSource.TypeDef,
            UIShaderSource.ConstDef,
            UIShaderSource.Group0,
            UIShaderSource.Group1,
            UIShaderSource.Group2,
            UIShaderSource.Fn_pow_x2,
            UIShaderSource.Fn_blend,
            UIShaderSource.Fn_vs_main,
            UIShaderSource.Fn_corner_area_color,
            UIShaderSource.Fn_get_texel_color,
            UIShaderSource.Fn_calc_background_brush_color,
            UIShaderSource.Fn_ui_color_shared_algorithm,
            UIShaderSource.Fn_gamma22,
            fs_main);
        return sb.Utf8String.ToArray();
    });

    private readonly Own<Texture2D> _emptyTexture;
    private readonly Own<Sampler> _emptyTextureSampler;

    public Texture2D EmptyTexture => _emptyTexture.AsValue();
    public Sampler EmptySampler => _emptyTextureSampler.AsValue();

    protected UIShader(Screen screen)
        : base(screen, new ShaderPassDescriptorArray1
        {
            Pass0 = new()
            {
                Source = _defaultShaderSource.Value,
                SortOrder = 3000,
                LayoutDescriptor = PipelineLayoutFactory(screen, out var diposable),
                PipelineDescriptorFactory = (module, layout) => PipelineFactory(module, layout, screen.Surface.Format, screen.DepthStencil.Format),
                PassKind = PassKind.Surface,
            },
        })
    {
        diposable.DisposeOn(Disposed);
        _emptyTexture = Texture2D.Create(screen, new()
        {
            Size = new(1, 1),
            Format = TextureFormat.Rgba8Unorm,
            MipLevelCount = 1,
            Usage = TextureUsages.TextureBinding | TextureUsages.CopySrc,
            SampleCount = 1,
        });
        _emptyTextureSampler = Sampler.Create(screen, new()
        {
            AddressModeU = AddressMode.ClampToEdge,
            AddressModeV = AddressMode.ClampToEdge,
            AddressModeW = AddressMode.ClampToEdge,
            MinFilter = FilterMode.Nearest,
            MagFilter = FilterMode.Nearest,
            MipmapFilter = FilterMode.Nearest,
        });
    }

    protected override void Release(bool manualRelease)
    {
        base.Release(manualRelease);
        _emptyTexture.Dispose();
        _emptyTextureSampler.Dispose();
    }

    public abstract Own<UIMaterial> CreateMaterial();

    private static PipelineLayoutDescriptor PipelineLayoutFactory(
        Screen screen,
        out DisposableBag disposable)
    {
        disposable = new DisposableBag();
        return new()
        {
            BindGroupLayouts =
            [
                BindGroupLayout.Create(screen, new()
                {
                    Entries =
                    [
                        BindGroupLayoutEntry.Buffer(0, ShaderStages.Vertex | ShaderStages.Fragment, new BufferBindingData { Type = BufferBindingType.Uniform }),
                        BindGroupLayoutEntry.Buffer(1, ShaderStages.Vertex | ShaderStages.Fragment, new BufferBindingData { Type = BufferBindingType.Uniform }),
                    ],
                }).AddTo(disposable),
                BindGroupLayout.Create(screen, new()
                {
                    Entries =
                    [
                        BindGroupLayoutEntry.Texture(0, ShaderStages.Vertex | ShaderStages.Fragment, new TextureBindingData
                        {
                            ViewDimension = TextureViewDimension.D2,
                            Multisampled = false,
                            SampleType = TextureSampleType.FloatNotFilterable,
                        }),
                        BindGroupLayoutEntry.Sampler(1, ShaderStages.Vertex | ShaderStages.Fragment, SamplerBindingType.NonFiltering),
                        BindGroupLayoutEntry.Buffer(2, ShaderStages.Vertex | ShaderStages.Fragment, new BufferBindingData { Type = BufferBindingType.Uniform }),
                    ],
                }).AddTo(disposable),
                BindGroupLayout.Create(screen, new()
                {
                    Entries =
                    [
                        BindGroupLayoutEntry.Buffer(0, ShaderStages.Vertex | ShaderStages.Fragment, new BufferBindingData { Type = BufferBindingType.StorageReadOnly }),
                    ],
                }).AddTo(disposable),
            ],
        };
    }

    private static RenderPipelineDescriptor PipelineFactory(ShaderModule module, PipelineLayout layout, TextureFormat surfaceFormat, TextureFormat depthStencilFormat)
    {
        return new RenderPipelineDescriptor
        {
            Layout = layout,
            Vertex = new VertexState()
            {
                Module = module,
                EntryPoint = "vs_main"u8.ToImmutableArray(),
                Buffers =
                [
                    VertexBufferLayout.FromVertex<VertexSlim>(
                    [
                        (0, VertexFieldSemantics.Position),
                        (1, VertexFieldSemantics.UV),
                    ]),
                ],
            },
            Fragment = new FragmentState()
            {
                Module = module,
                EntryPoint = "fs_main"u8.ToImmutableArray(),
                Targets =
                [
                    new ColorTargetState
                    {
                        Format = surfaceFormat,
                        Blend = BlendState.AlphaBlending,
                        WriteMask = ColorWrites.All,
                    },
                ],
            },
            Primitive = new PrimitiveState()
            {
                Topology = PrimitiveTopology.TriangleList,
                FrontFace = FrontFace.Ccw,
                CullMode = Face.Back,
                PolygonMode = PolygonMode.Fill,
                StripIndexFormat = null,
            },
            DepthStencil = new DepthStencilState
            {
                Format = depthStencilFormat,
                DepthWriteEnabled = true,
                DepthCompare = CompareFunction.GreaterEqual,
                Stencil = StencilState.Default,
                Bias = DepthBiasState.Default,
            },
            Multisample = MultisampleState.Default,
            Multiview = 0,
        };
    }
}
