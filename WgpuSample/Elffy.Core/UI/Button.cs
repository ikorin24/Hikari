#nullable enable
using Elffy;
using Elffy.Effective;
using Elffy.Internal;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Elffy.UI;

public sealed class Button : UIElement, IFromJson<Button>
{
    private ButtonInfo _buttonInfo;

    internal ref readonly ButtonInfo ButtonInfo => ref _buttonInfo;

    public string Text
    {
        get => _buttonInfo.Text;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if(value == _buttonInfo.Text) { return; }
            _buttonInfo.Text = value;
        }
    }

    public FontSize FontSize
    {
        get => _buttonInfo.FontSize;
        set
        {
            if(value == _buttonInfo.FontSize) { return; }
            _buttonInfo.FontSize = value;
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

    public static Button FromJson(in ObjectSource source) => new Button(source);

    protected override void ToJsonProtected(Utf8JsonWriter writer)
    {
        base.ToJsonProtected(writer);
        writer.WriteString(nameof(Text), Text);
        writer.Write(nameof(FontSize), FontSize);
    }

    protected override void ApplyDiffProtected(in ObjectSource source)
    {
        base.ApplyDiffProtected(source);
        Text = source.ApplyProperty(nameof(Text), Text, () => ButtonInfo.DefaultText, out _);
        FontSize = source.ApplyProperty(nameof(FontSize), FontSize, () => ButtonInfo.DefaultFontSize, out _);
    }

    public Button() : base()
    {
        _buttonInfo = ButtonInfo.Default;
    }

    private Button(in ObjectSource source) : base(source)
    {
        _buttonInfo = ButtonInfo.Default;
        if(source.TryGetProperty(nameof(Text), out var text)) {
            Text = Serializer.Instantiate<string>(text);
        }
        if(source.TryGetProperty(nameof(FontSize), out var fontSize)) {
            FontSize = Serializer.Instantiate<FontSize>(fontSize);
        }
    }
}

internal record struct ButtonInfo
{
    public required string Text { get; set; }
    public required FontSize FontSize { get; set; }

    public static ButtonInfo Default => new()
    {
        Text = DefaultText,
        FontSize = DefaultFontSize,
    };

    public static string DefaultText => "";
    public static FontSize DefaultFontSize => 16;
}

file sealed class ButtonShader : UIShader
{
    private static ReadOnlySpan<byte> Group1 => """
        @group(1) @binding(0) var tex: texture_2d<f32>;
        @group(1) @binding(1) var tex_sampler: sampler;
        """u8;

    private static ReadOnlySpan<byte> Fn_get_texel_color => """
        const TEXT_HALIGN_LEFT: u32 = 0u;
        const TEXT_HALIGN_CENTER: u32 = 1u;
        const TEXT_HALIGN_RIGHT: u32 = 2u;
        const TEXT_VALIGN_TOP: u32 = 0u;
        const TEXT_VALIGN_CENTER: u32 = 1u;
        const TEXT_VALIGN_BOTTOM: u32 = 2u;

        fn get_texel_color(
            f_pos: vec2<f32>, 
            h_align: u32, 
            v_align: u32,
            rect_pos: vec2<f32>,
            rect_size: vec2<f32>,
        ) -> vec4<f32> {
            let tex_size: vec2<i32> = textureDimensions(tex, 0).xy;
            var offset_in_rect: vec2<f32>;
            if(h_align == TEXT_HALIGN_CENTER) {
                offset_in_rect.x = (rect_size.x - vec2<f32>(tex_size).x) * 0.5;
            }
            else if(h_align == TEXT_HALIGN_RIGHT) {
                offset_in_rect.x = rect_size.x - vec2<f32>(tex_size).x;
            }
            else {
                // h_align == TEXT_HALIGN_LEFT
                offset_in_rect.x = 0.0;
            }

            if(v_align == TEXT_VALIGN_CENTER) {
                offset_in_rect.y = (rect_size.y - vec2<f32>(tex_size).y) * 0.5;
            }
            else if(v_align == TEXT_VALIGN_TOP) {
                offset_in_rect.y = 0.0;
            }
            else {
                // v_align == TEXT_VALIGN_BOTTOM
                offset_in_rect.y = rect_size.y - vec2<f32>(tex_size).y;
            }
            let texel_pos: vec2<f32> = f_pos - (rect_pos + offset_in_rect);
            if(texel_pos.x < 0.0 || texel_pos.x >= f32(tex_size.x) || texel_pos.y < 0.0 || texel_pos.y >= f32(tex_size.y)) {
                return vec4<f32>(0.0, 0.0, 0.0, 0.0);
            }
            else {
                return textureLoad(tex, vec2<i32>(texel_pos), 0);
            }
        }
        """u8;

    private readonly Own<Texture> _emptyTexture;
    private readonly Own<Sampler> _emptyTextureSampler;

    private ButtonShader(UILayer operation, ReadOnlySpan<byte> shaderSource)
        : base(shaderSource, operation, Desc)
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
        var fs_main = """
            fn calc_back_color(
                f_pos: vec2<f32>,
                pos: vec2<f32>,
                size: vec2<f32>,
            ) -> vec4<f32> {
                let texel_color = get_texel_color(f_pos, TEXT_HALIGN_CENTER, TEXT_VALIGN_CENTER, pos, size);
                return blend(texel_color, data.solid_color, 1.0);
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
            Group1,
            UIShaderSource.Fn_pow_x2,
            UIShaderSource.Fn_blend,
            UIShaderSource.Fn_vs_main,
            UIShaderSource.Fn_corner_area_color,
            Fn_get_texel_color,
            UIShaderSource.Fn_ui_color_shared_algorithm,
            fs_main);
        return CreateOwn(new ButtonShader(layer, sb.Utf8String));
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
        private BufferData? _bufferData;
        private ButtonInfo? _buttonInfo;

        [StructLayout(LayoutKind.Sequential, Pack = WgslConst.AlignOf_mat4x4_f32)]
        private readonly record struct BufferData
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
            if(_buttonInfo != button.ButtonInfo) {
                _buttonInfo = button.ButtonInfo;
                UpdateButtonTexture(button.ButtonInfo);
            }
            var bufferData = new BufferData
            {
                Mvp = result.MvpMatrix,
                SolidColor = result.BackgroundColor.SolidColor,
                Rect = result.ActualRect,
                BorderWidth = result.ActualBorderWidth,
                BorderRadius = result.ActualBorderRadius,
                BorderSolidColor = result.BorderColor.SolidColor,
            };
            if(_bufferData != bufferData) {
                _bufferData = bufferData;
                WriteUniform(bufferData);
            }
        }

        private void UpdateButtonTexture(in ButtonInfo button)
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
