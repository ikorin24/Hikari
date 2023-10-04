#nullable enable
using Hikari.Internal;
using System;

namespace Hikari.UI;

internal abstract class UIShader : Shader<UIShader, UIMaterial, UILayer>
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

    protected UIShader(UILayer operation)
    : this(_defaultShaderSource.Value, operation, DefaultPipelineDescCreator)
    {
    }

    protected UIShader(
        ReadOnlySpan<byte> shaderSource,
        UILayer operation,
        Func<UILayer, ShaderModule, RenderPipelineDescriptor> getPipelineDesc)
        : base(shaderSource, operation, getPipelineDesc)
    {
        _emptyTexture = Texture2D.Create(operation.Screen, new()
        {
            Format = TextureFormat.Rgba8UnormSrgb,
            MipLevelCount = 1,
            SampleCount = 1,
            Size = new Vector2u(1, 1),
            Usage = TextureUsages.TextureBinding,
        });
        _emptyTextureSampler = Sampler.Create(operation.Screen, new SamplerDescriptor
        {
            AddressModeU = AddressMode.ClampToEdge,
            AddressModeV = AddressMode.ClampToEdge,
            AddressModeW = AddressMode.ClampToEdge,
            MagFilter = FilterMode.Nearest,
            MinFilter = FilterMode.Nearest,
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

    private static RenderPipelineDescriptor DefaultPipelineDescCreator(UILayer layer, ShaderModule module)
    {
        return new RenderPipelineDescriptor
        {
            Layout = layer.PipelineLayout,
            Vertex = new VertexState()
            {
                Module = module,
                EntryPoint = "vs_main"u8.ToArray(),
                Buffers = new VertexBufferLayout[]
                {
                    VertexBufferLayout.FromVertex<VertexSlim>(stackalloc[]
                    {
                        (0, VertexFieldSemantics.Position),
                        (1, VertexFieldSemantics.UV),
                    }),
                },
            },
            Fragment = new FragmentState()
            {
                Module = module,
                EntryPoint = "fs_main"u8.ToArray(),
                Targets = new ColorTargetState?[]
                {
                    new ColorTargetState
                    {
                        Format = layer.ColorFormat,
                        Blend = BlendState.AlphaBlending,
                        WriteMask = ColorWrites.All,
                    },
                },
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
                Format = layer.DepthStencilFormat,
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
