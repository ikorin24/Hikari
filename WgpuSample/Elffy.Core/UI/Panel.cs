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
    private static ReadOnlySpan<byte> Fn_fs_main => """
        fn corner_area_color(
            f_pos: vec2<f32>, 
            center: vec2<f32>, 
            b_radius: f32, 
            bw: vec2<f32>, 
            back_color: vec4<f32>,
            b_color: vec4<f32>,
        ) -> vec4<f32> {
            let d: vec2<f32> = f_pos + vec2<f32>(0.5, 0.5) - center;
            let len_d = length(d);
            let a = saturate(b_radius - len_d + 0.5);
            if(a <= 0.0) {
                discard;
            }
            if(bw.x == 0.0 && bw.y == 0.0) {
                // To avoid a slight border due to float errors, I don't draw the border color.
                return vec4<f32>(back_color.rgb, back_color.a * a);
            }
            var er_x: f32 = max(0.001, b_radius - bw.x);   // x-axis radius of ellipse
            var er_y: f32 = max(0.001, b_radius - bw.y);   // y-axis radius of ellipse
            // vector from center of ellipse to the crossed point of 'd' and the ellipse
            let v: vec2<f32> = d * er_x * er_y / sqrt(pow_x2(er_y * d.x) + pow_x2(er_x * d.y));
            let b = saturate(len_d - length(v) + 0.5);
            let b_color_blend = blend(b_color, back_color, b);
            return vec4<f32>(b_color_blend.rgb, b_color_blend.a * a);
        }

        @fragment fn fs_main(
            f: V2F,
        ) -> @location(0) vec4<f32> {
            // pixel coordinates, which is not normalized
            let f_pos: vec2<f32> = floor(f.clip_pos.xy);
            //let f_pos: vec2<f32> = f.clip_pos.xy - fract(f.clip_pos.xy);
            var back_color: vec4<f32> = data.solid_color;
            let b_color: vec4<f32> = data.border_solid_color;
            let b_radius = array<f32, 4>(
                round(data.border_radius.x),
                round(data.border_radius.y),
                round(data.border_radius.z),
                round(data.border_radius.w),
            );
            let b_width = array<f32, 4>(
                round(data.border_width.x),
                round(data.border_width.y),
                round(data.border_width.z),
                round(data.border_width.w),
            );
            let pos: vec2<f32> = round(data.rect.xy);
            let size: vec2<f32> = round(data.rect.zw);

            let center = array<vec2<f32>, 4>(
                pos + vec2<f32>(b_radius[0], b_radius[0]),
                pos + vec2<f32>(size.x - b_radius[1], b_radius[1]),
                pos + vec2<f32>(size.x - b_radius[2], size.y - b_radius[2]),
                pos + vec2<f32>(b_radius[3], size.y - b_radius[3]),
            );
            // outside of the rectangle
            if(f_pos.x < pos.x || f_pos.x >= pos.x + size.x || f_pos.y < pos.y || f_pos.y >= pos.y + size.y) {
                discard;
            }

            // top-left corner
            if(f_pos.x < center[0].x && f_pos.y < center[0].y) {
                return corner_area_color(
                    f_pos, center[0], b_radius[0],
                    vec2<f32>(b_width[3], b_width[0]),
                    back_color, b_color,
                );
            }
            // top-right corner
            else if(f_pos.x >= center[1].x && f_pos.y < center[1].y) {
                return corner_area_color(
                    f_pos, center[1], b_radius[1],
                    vec2<f32>(b_width[1], b_width[0]),
                    back_color, b_color,
                );
            }
            // bottom-right corner
            else if(f_pos.x >= center[2].x && f_pos.y >= center[2].y) {
                return corner_area_color(
                    f_pos, center[2], b_radius[2],
                    vec2<f32>(b_width[1], b_width[2]),
                    back_color, b_color,
                );
            }
            // bottom-left corner
            else if(f_pos.x < center[3].x && f_pos.y >= center[3].y) {
                return corner_area_color(
                    f_pos, center[3], b_radius[3],
                    vec2<f32>(b_width[3], b_width[2]),
                    back_color, b_color,
                );
            }
            // side border
            else if(
                f_pos.y < pos.y + b_width[0] || 
                f_pos.x >= pos.x + size.x - b_width[1] || 
                f_pos.y >= pos.y + size.y - b_width[2] || 
                f_pos.x < pos.x + b_width[3]
            ) {
                return b_color;
            }

            return back_color;
        }
        """u8;

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
        using var sb = Utf8StringBuilder.FromLines(
            UIShaderSource.TypeDef,
            UIShaderSource.ConstDef,
            UIShaderSource.Group0,
            UIShaderSource.Fn_pow_x2,
            UIShaderSource.Fn_blend,
            UIShaderSource.Fn_vs_main,
            Fn_fs_main);
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
