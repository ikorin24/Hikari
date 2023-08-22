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
                let bg_color = calc_background_brush_color(f_pos, pos, size);
                return blend(texel_color, bg_color, 1.0);
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
            UIShaderSource.Group1,
            UIShaderSource.Group2,
            UIShaderSource.Fn_pow_x2,
            UIShaderSource.Fn_blend,
            UIShaderSource.Fn_vs_main,
            UIShaderSource.Fn_corner_area_color,
            UIShaderSource.Fn_get_texel_color,
            UIShaderSource.Fn_calc_background_brush_color,
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
        private ButtonInfo? _buttonInfo;

        private Material(
            UIShader shader,
            MaybeOwn<Texture> texture,
            MaybeOwn<Sampler> sampler)
            : base(shader, texture, sampler)
        {
        }

        internal static Own<Material> Create(UIShader shader, MaybeOwn<Texture> texture, MaybeOwn<Sampler> sampler)
        {
            var self = new Material(shader, texture, sampler);
            return CreateOwn(self);
        }

        public override void UpdateMaterial(UIElement element, in UIUpdateResult result)
        {
            base.UpdateMaterial(element, result);
            var button = (Button)element;
            if(_buttonInfo != button.ButtonInfo) {
                _buttonInfo = button.ButtonInfo;
                UpdateButtonTexture(button.ButtonInfo);
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
                    self.UpdateTexture(texture);
                });
            }
        }
    }
}
