#nullable enable
using Elffy.Internal;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Elffy.UI;

public sealed class Panel : UIElement, IFromJson<Panel>
{
    static Panel()
    {
        Serializer.RegisterConstructor(FromJson);
        UILayer.RegisterShader<Panel>(static layer =>
        {
            return PanelShader.Create(layer).Cast<UIShader>();
        });
    }

    public static Panel FromJson(in ObjectSource source) => new Panel(source);

    public Panel()
    {
    }

    private Panel(in ObjectSource source) : base(source)
    {
    }

    protected override void ToJsonProtected(Utf8JsonWriter writer)
    {
        base.ToJsonProtected(writer);
    }

    protected override void ApplyDiffProtected(in ObjectSource source)
    {
        base.ApplyDiffProtected(source);
    }
}

file sealed class PanelShader : UIShader
{
    private readonly Own<Texture> _emptyTexture;
    private readonly Own<Sampler> _emptyTextureSampler;

    private PanelShader(UILayer operation, ReadOnlySpan<byte> shaderSource)
        : base(shaderSource, operation, Desc)
    {
        _emptyTexture = Texture.Create(operation.Screen, new TextureDescriptor
        {
            Dimension = TextureDimension.D2,
            Format = TextureFormat.Rgba8Unorm,
            MipLevelCount = 1,
            SampleCount = 1,
            Size = new Vector3u(1, 1, 1),
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

    private static RenderPipelineDescriptor Desc(PipelineLayout layout, ShaderModule module)
    {
        var screen = layout.Screen;
        return new RenderPipelineDescriptor
        {
            Layout = layout,
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
                        Format = screen.SurfaceFormat,
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
            DepthStencil = null,
            Multisample = MultisampleState.Default,
            Multiview = 0,
        };
    }

    public static Own<PanelShader> Create(UILayer layer)
    {
        var fs_main = """
            fn calc_back_color(
                f_pos: vec2<f32>,
                pos: vec2<f32>,
                size: vec2<f32>,
            ) -> vec4<f32> {
                return data.solid_color;
            }

            @fragment fn fs_main(
                f: V2F,
            ) -> @location(0) vec4<f32> {
                return ui_color_shared_algorithm(f);
            }
            """u8;

        using var sb = Utf8StringBuilder.FromLines(
            UIShaderSource.TypeDef,
            UIShaderSource.ConstDef,
            UIShaderSource.Group0,
            UIShaderSource.Fn_pow_x2,
            UIShaderSource.Fn_blend,
            UIShaderSource.Fn_vs_main,
            UIShaderSource.Fn_corner_area_color,
            UIShaderSource.Fn_ui_color_shared_algorithm,
            fs_main);
        return CreateOwn(new PanelShader(layer, sb.Utf8String));
    }

    public override Own<UIMaterial> CreateMaterial()
    {
        return PanelShader.Material.Create(this, _emptyTexture.AsValue(), _emptyTextureSampler.AsValue()).Cast<UIMaterial>();
    }

    private sealed class Material : UIMaterial
    {
        private readonly Own<Buffer> _buffer;
        private readonly Own<BindGroup> _bindGroup0;
        private readonly Own<BindGroup> _bindGroup1;

        [StructLayout(LayoutKind.Sequential, Pack = WgslConst.AlignOf_mat4x4_f32)]
        private readonly struct BufferData
        {
            public required Matrix4 Mvp { get; init; }
            public required Color4 SolidColor { get; init; }
            public required RectF Rect { get; init; }
            public required Vector4 BorderWidth { get; init; }
            public required Vector4 BorderRadius { get; init; }
            public required Color4 BorderSolidColor { get; init; }
        }

        public override BindGroup BindGroup0 => _bindGroup0.AsValue();
        public override BindGroup BindGroup1 => _bindGroup1.AsValue();

        private Material(
            UIShader shader,
            Own<BindGroup> bindGroup0,
            Own<BindGroup> bindGroup1,
            Own<Buffer> buffer)
            : base(shader)
        {
            _bindGroup0 = bindGroup0;
            _bindGroup1 = bindGroup1;
            _buffer = buffer;
        }

        protected override void Release(bool manualRelease)
        {
            base.Release(manualRelease);
            if(manualRelease) {
                _bindGroup0.Dispose();
                _bindGroup1.Dispose();
                _buffer.Dispose();
            }
        }

        private void WriteUniform(in BufferData data)
        {
            var buffer = _buffer.AsValue();
            buffer.WriteData(0, data);
        }

        internal static Own<Material> Create(UIShader shader, Texture texture, Sampler sampler)
        {
            var screen = shader.Screen;
            var buffer = Buffer.Create(screen, (nuint)Unsafe.SizeOf<BufferData>(), BufferUsages.Uniform | BufferUsages.CopyDst);
            var bindGroup0 = BindGroup.Create(screen, new()
            {
                Layout = shader.Operation.BindGroupLayout0,
                Entries = new[]
                {
                    BindGroupEntry.Buffer(0, screen.InfoBuffer),
                    BindGroupEntry.Buffer(1, buffer.AsValue()),
                },
            });
            var bindGroup1 = BindGroup.Create(screen, new()
            {
                Layout = shader.Operation.BindGroupLayout1,
                Entries = new[]
                {
                    BindGroupEntry.TextureView(0, texture.View),
                    BindGroupEntry.Sampler(1, sampler),
                },
            });
            var self = new Material(shader, bindGroup0, bindGroup1, buffer);
            return CreateOwn(self);
        }

        public override void UpdateMaterial(UIElement element, in UIUpdateResult result)
        {
            WriteUniform(new()
            {
                Mvp = result.MvpMatrix,
                SolidColor = result.BackgroundColor.SolidColor,
                Rect = result.ActualRect,
                BorderWidth = result.ActualBorderWidth,
                BorderRadius = result.ActualBorderRadius,
                BorderSolidColor = result.BorderColor.SolidColor,
            });
        }
    }
}
