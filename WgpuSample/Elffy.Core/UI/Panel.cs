#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;

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

    public static Panel FromJson(in ReactSource source) => new Panel(source);

    public Panel()
    {
    }

    private Panel(in ReactSource source) : base(source)
    {
    }

    protected override JsonNode ToJsonProtected()
    {
        var node = base.ToJsonProtected();
        return node;
    }

    protected override void ApplyDiffProtected(in ReactSource source)
    {
        base.ApplyDiffProtected(source);
    }
}

file sealed class PanelShader : UIShader
{
    private static ReadOnlySpan<byte> ShaderSource => """
        struct Vin {
            @location(0) pos: vec3<f32>,
            @location(1) uv: vec2<f32>,
        }
        struct V2F {
            @builtin(position) clip_pos: vec4<f32>,
            @location(0) uv: vec2<f32>,
        }
        struct ScreenInfo {
            size: vec2<u32>,
        }
        struct BufferData
        {
            mvp: mat4x4<f32>,
            solid_color: vec4<f32>,
            rect: vec4<f32>,            // (x, y, width, height)
            border_width: vec4<f32>,    // (top, right, bottom, left)
            border_radius: vec4<f32>,   // (top-left, top-right, bottom-right, bottom-left)
            border_solid_color: vec4<f32>,
        }

        @group(0) @binding(0) var<uniform> screen: ScreenInfo;
        @group(0) @binding(1) var<uniform> data: BufferData;

        const PI: f32 = 3.141592653589793;
        const INV_PI: f32 = 0.3183098861837907;

        @vertex fn vs_main(
            v: Vin,
        ) -> V2F {
            var o: V2F;
            o.clip_pos = data.mvp * vec4<f32>(v.pos, 1.0);
            o.uv = v.uv;
            return o;
        }

        fn pow_x2(x: f32) -> f32 {
            return x * x;
        }

        fn blend(src: vec4<f32>, dst: vec4<f32>, x: f32) -> vec4<f32> {
            let a = src.a * x;
            return vec4(
                src.rgb * a + (1.0 - a) * dst.rgb,
                a + (1.0 - a) * dst.a,
            );
        }

        @fragment fn fs_main(
            f: V2F,
        ) -> @location(0) vec4<f32> {
            // pixel coordinates, which is not normalized
            let fragcoord: vec2<f32> = f.clip_pos.xy;
            var color: vec4<f32> = data.solid_color;

            let b_radius = data.border_radius;
            let b_color = array<vec4<f32>, 4>(
                blend(data.border_solid_color, color, min(1.0, data.border_width.x)),
                blend(data.border_solid_color, color, min(1.0, data.border_width.y)),
                blend(data.border_solid_color, color, min(1.0, data.border_width.z)),
                blend(data.border_solid_color, color, min(1.0, data.border_width.w)),
            );

            // top-left corner
            let center_tl = data.rect.xy + vec2<f32>(b_radius.x, b_radius.x);
            if(fragcoord.x < center_tl.x && fragcoord.y < center_tl.y) {
                let d = fragcoord - center_tl;
                let len_d = length(d);
                if(len_d > b_radius.x + 1.0) {
                    discard;
                }
                if(len_d > b_radius.x) {
                    return blend(b_color[0], vec4<f32>(), 1.0 - (len_d - b_radius.x));
                }
                var a = b_radius.x - data.border_width.w;   // x-axis radius of ellipse
                var b = b_radius.x - data.border_width.x;   // y-axis radius of ellipse

                // vector from center of ellipse to the crossed point of 'd' and the ellipse
                let v: vec2<f32> = d * a * b / sqrt(pow_x2(b * d.x) + pow_x2(a * d.y));
                let len_v = length(v);
                var blend_coeff = max(0.0, min(1.0, len_d - len_v));
                if(u32(fragcoord.y) <= u32(data.rect.y) + u32(data.border_width.x) - 1u) {
                    // It's heuristic for error of float number
                    blend_coeff = 1.0;
                }
                if(u32(fragcoord.x) <= u32(data.rect.x) + u32(data.border_width.x) - 1u) {
                    // It's heuristic for error of float number
                    blend_coeff = 1.0;
                }
                return blend(b_color[0], color, blend_coeff);
            }

            // top-right corner
            let center_tr = data.rect.xy + vec2<f32>(data.rect.z - b_radius.y, b_radius.y);
            if(fragcoord.x >= center_tr.x && fragcoord.y < center_tr.y) {
                let d = fragcoord - center_tr;
                let len_d = length(d);
                if(len_d > b_radius.x + 1.0) {
                    discard;
                }
                if(len_d > b_radius.x) {
                    return vec4<f32>(
                        data.border_solid_color.rgb, 
                        data.border_solid_color.a * (1.0 - (len_d - b_radius.x)),
                    );
                }
                var a = b_radius.y - data.border_width.y;   // x-axis radius of ellipse
                var b = b_radius.y - data.border_width.x;   // y-axis radius of ellipse

                // vector from center of ellipse to the crossed point of 'd' and the ellipse
                let v: vec2<f32> = d * a * b / sqrt(pow_x2(b * d.x) + pow_x2(a * d.y));
                let len_v = length(v);
                if(len_d > len_v) {
                    return data.border_solid_color;
                }
                let diff = len_v - len_d;
                if(diff <= 1.0) {
                    return blend(data.border_solid_color, color, 1.0 - diff);
                }
            }

            // bottom-right corner
            let center_br = data.rect.xy + vec2<f32>(data.rect.z - b_radius.z, data.rect.w - b_radius.z);
            if(fragcoord.x >= center_br.x && fragcoord.y >= center_br.y) {
                let d = fragcoord - center_br;
                let len_d = length(d);
                if(len_d > b_radius.x + 1.0) {
                    discard;
                }
                if(len_d > b_radius.x) {
                    return vec4<f32>(
                        data.border_solid_color.rgb, 
                        data.border_solid_color.a * (1.0 - (len_d - b_radius.x)),
                    );
                }
                var a = b_radius.z - data.border_width.y;   // x-axis radius of ellipse
                var b = b_radius.z - data.border_width.z;   // y-axis radius of ellipse

                // vector from center of ellipse to the crossed point of 'd' and the ellipse
                let v: vec2<f32> = d * a * b / sqrt(pow_x2(b * d.x) + pow_x2(a * d.y));
                let len_v = length(v);
                if(len_d > len_v) {
                    return data.border_solid_color;
                }
                let diff = len_v - len_d;
                if(diff <= 1.0) {
                    return blend(data.border_solid_color, color, 1.0 - diff);
                }
            }

            // bottom-left corner
            let center_bl = data.rect.xy + vec2<f32>(b_radius.w, data.rect.w - b_radius.w);
            if(fragcoord.x < center_bl.x && fragcoord.y >= center_bl.y) {
                let d = fragcoord - center_bl;
                let len_d = length(d);
                if(len_d > b_radius.x + 1.0) {
                    discard;
                }
                if(len_d > b_radius.x) {
                    return vec4<f32>(
                        data.border_solid_color.rgb, 
                        data.border_solid_color.a * (1.0 - (len_d - b_radius.x)),
                    );
                }
                var a = b_radius.z - data.border_width.w;   // x-axis radius of ellipse
                var b = b_radius.z - data.border_width.z;   // y-axis radius of ellipse

                // vector from center of ellipse to the crossed point of 'd' and the ellipse
                let v: vec2<f32> = d * a * b / sqrt(pow_x2(b * d.x) + pow_x2(a * d.y));
                let len_v = length(v);
                if(len_d > len_v) {
                    return data.border_solid_color;
                }
                let diff = len_v - len_d;
                if(diff <= 1.0) {
                    return blend(data.border_solid_color, color, 1.0 - diff);
                }
            }

            // top border
            if(fragcoord.y < data.rect.y + data.border_width.x) {
                return b_color[0];
            }
            // right border
            if(fragcoord.x >= data.rect.x + data.rect.z - data.border_width.y) {
                return data.border_solid_color;
            }
            // left border
            if(fragcoord.x < data.rect.x + data.border_width.w) {
                return data.border_solid_color;
            }
            // bottom border
            if(fragcoord.y >= data.rect.y + data.rect.w - data.border_width.z) {
                return data.border_solid_color;
            }
            return color;
        }
        """u8;

    private readonly Own<Texture> _emptyTexture;
    private readonly Own<Sampler> _emptyTextureSampler;

    private PanelShader(UILayer operation)
        : base(ShaderSource, operation, Desc)
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
        return CreateOwn(new PanelShader(layer));
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
