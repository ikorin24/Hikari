#nullable enable
using Elffy;
using Elffy.Effective;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Elffy.UI;

public sealed class Button : UIElement, IFromJson<Button>
{
    private string _text;
    private FontSize _fontSize;
    private EventSource<Button> _clicked;
    private bool _isClickHolding = false;

    private static string DefaultText => "";
    private static FontSize DefaultFontSize => 16;

    public Event<Button> Clicked => _clicked.Event;

    public string Text
    {
        get => _text;
        set
        {
            if(value == _text) { return; }
            _text = value;
            RequestUpdateMaterial();
        }
    }

    public FontSize FontSize
    {
        get => _fontSize;
        set
        {
            if(value == _fontSize) { return; }
            _fontSize = value;
            RequestUpdateMaterial();
        }
    }

    static Button()
    {
        Serializer.RegisterConstructor(FromJson);
        UILayer.RegisterShader<Button>(static layer =>
        {
            return ButtonShader.Create(layer).Cast<UIShader>();
        });
    }

    public static Button FromJson(in ReactSource source) => new Button(source);

    protected override void ToJsonProtected(Utf8JsonWriter writer)
    {
        base.ToJsonProtected(writer);
        writer.WriteString(nameof(Text), _text);
        writer.Write(nameof(FontSize), _fontSize);
    }

    protected override void ApplyDiffProtected(in ReactSource source)
    {
        base.ApplyDiffProtected(source);
        Text = source.ApplyProperty(nameof(Text), Text, () => DefaultText, out _);
        FontSize = source.ApplyProperty(nameof(FontSize), FontSize, () => DefaultFontSize, out _);
    }

    public Button() : base()
    {
        _text = DefaultText;
        _fontSize = DefaultFontSize;
    }

    private Button(in ReactSource source) : base(source)
    {
        _text = DefaultText;
        _fontSize = DefaultFontSize;
        if(source.TryGetProperty(nameof(Text), out var text)) {
            _text = Serializer.Instantiate<string>(text);
        }
        if(source.TryGetProperty(nameof(FontSize), out var fontSize)) {
            _fontSize = Serializer.Instantiate<FontSize>(fontSize);
        }
        if(source.TryGetProperty(nameof(Clicked), out var clicked)) {
            var action = clicked.Instantiate<Action<Button>>();
            _clicked.Event.Subscribe(action);
        }
        ModelUpdate.Subscribe(model => OnModelUpdate(model));
    }

    private void OnModelUpdate(UIModel model)
    {
        var mouse = model.Screen.Mouse;
        HandleButtonClick(mouse);
    }

    private void HandleButtonClick(Mouse mouse)
    {
        var isHover = IsHover;
        if(_isClickHolding) {
            if(mouse.IsPressed(MouseButton.Left)) {
                return;
            }
            else if(mouse.IsUp(MouseButton.Left)) {
                _isClickHolding = false;
                if(isHover) {
                    _clicked.Invoke(this);
                }
                return;
            }
        }
        else {
            if(isHover && mouse.IsDown(MouseButton.Left)) {
                _isClickHolding = true;
                return;
            }
        }
    }
}

file sealed class ButtonShader : UIShader
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
        @group(1) @binding(0) var tex: texture_2d<f32>;
        @group(1) @binding(1) var tex_sampler: sampler;

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

        const TEXT_HALIGN_LEFT: u32 = 0u;
        const TEXT_HALIGN_CENTER: u32 = 1u;
        const TEXT_HALIGN_RIGHT: u32 = 2u;
        const TEXT_VALIGN_TOP: u32 = 0u;
        const TEXT_VALIGN_CENTER: u32 = 1u;
        const TEXT_VALIGN_BOTTOM: u32 = 2u;

        fn get_texel_color(fragcoord: vec2<f32>, h_align: u32, v_align: u32) -> vec4<f32> {
            let tex_size: vec2<i32> = textureDimensions(tex, 0).xy;
            var offset_in_rect: vec2<f32>;
            if(h_align == TEXT_HALIGN_CENTER) {
                offset_in_rect.x = (data.rect.z - vec2<f32>(tex_size).x) * 0.5;
            }
            else if(h_align == TEXT_HALIGN_RIGHT) {
                offset_in_rect.x = data.rect.z - vec2<f32>(tex_size).x;
            }
            else {
                // h_align == TEXT_HALIGN_LEFT
                offset_in_rect.x = 0.0;
            }

            if(v_align == TEXT_VALIGN_CENTER) {
                offset_in_rect.y = (data.rect.w - vec2<f32>(tex_size).y) * 0.5;
            }
            else if(v_align == TEXT_VALIGN_TOP) {
                offset_in_rect.y = 0.0;
            }
            else {
                // v_align == TEXT_VALIGN_BOTTOM
                offset_in_rect.y = data.rect.w - vec2<f32>(tex_size).y;
            }
            let texel_pos: vec2<f32> = fragcoord - (data.rect.xy + offset_in_rect);
            if(texel_pos.x < 0.0 || texel_pos.x >= f32(tex_size.x) || texel_pos.y < 0.0 || texel_pos.y >= f32(tex_size.y)) {
                return vec4<f32>(0.0, 0.0, 0.0, 0.0);
            }
            else {
                return textureLoad(tex, vec2<i32>(texel_pos), 0);
            }
        }

        @fragment fn fs_main(
            f: V2F,
        ) -> @location(0) vec4<f32> {
            // pixel coordinates, which is not normalized
            let fragcoord: vec2<f32> = f.clip_pos.xy;
            let texel_color = get_texel_color(fragcoord, TEXT_HALIGN_CENTER, TEXT_VALIGN_CENTER);
            var color: vec4<f32> = blend(texel_color, data.solid_color, 1.0);

            let b_radius = data.border_radius;

            // top-left corner
            let center_tl = data.rect.xy + vec2<f32>(b_radius.x, b_radius.x);
            if(fragcoord.x < center_tl.x && fragcoord.y < center_tl.y) {
                let d = fragcoord - center_tl;
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
                var a = b_radius.x - data.border_width.w;   // x-axis radius of ellipse
                var b = b_radius.x - data.border_width.x;   // y-axis radius of ellipse

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
                return data.border_solid_color;
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

    private ButtonShader(UILayer operation)
        : base(ShaderSource, operation, Desc)
    {
        _emptyTexture = Texture.Create(operation.Screen, new TextureDescriptor
        {
            Dimension = TextureDimension.D2,
            Format = TextureFormat.Rgba8UnormSrgb,
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

    public static Own<ButtonShader> Create(UILayer layer)
    {
        return CreateOwn(new ButtonShader(layer));
    }

    public override Own<UIMaterial> CreateMaterial()
    {
        return ButtonShader.Material.Create(this, _emptyTexture.AsValue(), _emptyTextureSampler.AsValue()).Cast<UIMaterial>();
    }

    private sealed class Material : UIMaterial
    {
        private readonly Own<Buffer> _buffer;
        private readonly Own<BindGroup> _bindGroup0;
        private Own<BindGroup> _bindGroup1;
        private MaybeOwn<Texture> _texture;
        private readonly MaybeOwn<Sampler> _sampler;

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
            Own<Buffer> buffer,
            MaybeOwn<Texture> texture,
            MaybeOwn<Sampler> sampler)
            : base(shader)
        {
            _bindGroup0 = bindGroup0;
            _bindGroup1 = bindGroup1;
            _texture = texture;
            _buffer = buffer;
            _sampler = sampler;
        }

        public override void Validate()
        {
            base.Validate();
            _texture.Validate();
            _sampler.Validate();
        }

        protected override void Release(bool manualRelease)
        {
            base.Release(manualRelease);
            if(manualRelease) {
                _bindGroup0.Dispose();
                _bindGroup1.Dispose();
                _buffer.Dispose();
                _texture.Dispose();
                _sampler.Dispose();
            }
        }

        private void WriteUniform(in BufferData data)
        {
            var buffer = _buffer.AsValue();
            buffer.WriteData(0, data);
        }

        internal static Own<Material> Create(UIShader shader, MaybeOwn<Texture> texture, MaybeOwn<Sampler> sampler)
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
            var bindGroup1 = CreateBindGroup1(screen, shader.Operation.BindGroupLayout1, texture.AsValue().View, sampler.AsValue());
            var self = new Material(shader, bindGroup0, bindGroup1, buffer, texture, sampler);
            return CreateOwn(self);
        }

        private static Own<BindGroup> CreateBindGroup1(Screen screen, BindGroupLayout layout, TextureView textureView, Sampler sampler)
        {
            return BindGroup.Create(screen, new()
            {
                Layout = layout,
                Entries = new[]
                {
                    BindGroupEntry.TextureView(0, textureView),
                    BindGroupEntry.Sampler(1, sampler),
                },
            });
        }

        public override void UpdateMaterial(UIElement element, in UIUpdateResult result)
        {
            var button = (Button)element;
            UpdateForButton(button);
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

        private void UpdateForButton(Button button)
        {
            var text = button.Text;
            if(string.IsNullOrEmpty(text) == false) {
                using var font = new SkiaSharp.SKFont();
                font.Size = button.FontSize.Px;
                var options = new TextDrawOptions
                {
                    Background = ColorByte.Transparent,
                    Foreground = ColorByte.Black,
                    Font = font,
                };
                TextDrawer.Draw(text, options, this, static (image, x) =>
                {
                    var (self, metrics) = x;
                    var texture = Texture.CreateFromRawData(self.Shader.Screen, new TextureDescriptor
                    {
                        Dimension = TextureDimension.D2,
                        Format = TextureFormat.Rgba8Unorm,
                        MipLevelCount = 1,
                        SampleCount = 1,
                        Size = new Vector3u((uint)image.Width, (uint)image.Height, 1),
                        Usage = TextureUsages.TextureBinding,
                    }, image.GetPixels().AsBytes());

                    var bindGroup1 = CreateBindGroup1(self.Screen, self.Operation.BindGroupLayout1, texture.AsValue().View, self._sampler.AsValue());
                    self._bindGroup1.Dispose();
                    self._texture.Dispose();
                    self._bindGroup1 = bindGroup1;
                    self._texture = texture;
                });
            }
        }
    }
}
