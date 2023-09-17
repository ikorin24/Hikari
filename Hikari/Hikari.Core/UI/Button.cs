#nullable enable
using Hikari.Internal;
using Hikari.Mathematics;
using System;
using System.Diagnostics;
using System.Text.Json;

namespace Hikari.UI;

public sealed class Button : UIElement, IFromJson<Button>
{
    private ButtonInfo _info;
    private ButtonPseudoInfo? _hoverInfo;
    private ButtonPseudoInfo? _activeInfo;
    private ButtonInfo? _appliedInfo;

    internal ref readonly ButtonInfo? ButtonApplied => ref _appliedInfo;

    public string Text
    {
        get => _info.Text;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if(value == _info.Text) { return; }
            _info.Text = value;
        }
    }

    public FontSize FontSize
    {
        get => _info.FontSize;
        set
        {
            if(value == _info.FontSize) { return; }
            _info.FontSize = value;
        }
    }

    public ButtonPseudoInfo? HoverProps
    {
        get => GetHoverProps();
        set
        {
            if(_hoverInfo == value) { return; }
            _hoverInfo = value;
            RequestRelayout();
        }
    }

    public ButtonPseudoInfo? ActiveProps
    {
        get => _activeInfo;
        set
        {
            if(_activeInfo == value) { return; }
            _activeInfo = value;
            RequestRelayout();
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
        _info = ButtonInfo.Default;
    }

    private Button(in ObjectSource source) : base(source)
    {
        _info = ButtonInfo.Default;
        if(source.TryGetProperty(nameof(Text), out var text)) {
            Text = Serializer.Instantiate<string>(text);
        }
        if(source.TryGetProperty(nameof(FontSize), out var fontSize)) {
            FontSize = Serializer.Instantiate<FontSize>(fontSize);
        }
        if(source.TryGetProperty(PseudoInfo.HoverName, out var hover)) {
            _hoverInfo = ButtonPseudoInfo.FromJson(hover);
        }
        if(source.TryGetProperty(PseudoInfo.ActiveName, out var active)) {
            _activeInfo = ButtonPseudoInfo.FromJson(active);
        }
    }

    protected override ButtonPseudoInfo? GetHoverProps() => _hoverInfo;

    protected override ButtonPseudoInfo? GetActiveProps() => _activeInfo;

    protected override void OnUpdateLayout(PseudoFlags flags)
    {
        var info = _info;
        if(flags.HasFlag(PseudoFlags.Hover) && _hoverInfo is not null) {
            info = info.Merged(_hoverInfo);
        }
        if(flags.HasFlag(PseudoFlags.Active) && _activeInfo is not null) {
            info = info.Merged(_activeInfo);
        }
        _appliedInfo = info;
    }
}

public sealed record ButtonPseudoInfo
    : PseudoInfo,
    IFromJson<ButtonPseudoInfo>
{
    static ButtonPseudoInfo() => Serializer.RegisterConstructor(FromJson);

    public string? Text { get; init; }
    public FontSize? FontSize { get; init; }

    public static ButtonPseudoInfo FromJson(in ObjectSource source)
    {
        return new ButtonPseudoInfo
        {
            Width = GetValueProp<LayoutLength>(source, nameof(Width)),
            Height = GetValueProp<LayoutLength>(source, nameof(Height)),
            Margin = GetValueProp<Thickness>(source, nameof(Margin)),
            Padding = GetValueProp<Thickness>(source, nameof(Padding)),
            HorizontalAlignment = GetValueProp<HorizontalAlignment>(source, nameof(HorizontalAlignment)),
            VerticalAlignment = GetValueProp<VerticalAlignment>(source, nameof(VerticalAlignment)),
            Background = GetValueProp<Brush>(source, nameof(Background)),
            BorderWidth = GetValueProp<Thickness>(source, nameof(BorderWidth)),
            BorderRadius = GetValueProp<CornerRadius>(source, nameof(BorderRadius)),
            BorderColor = GetValueProp<Brush>(source, nameof(BorderColor)),
            BoxShadow = GetValueProp<BoxShadow>(source, nameof(BoxShadow)),
            Flow = GetValueProp<Flow>(source, nameof(Flow)),
            Color = GetValueProp<Color4>(source, nameof(Color)),
            Text = GetClassProp<string>(source, nameof(Text)),
            FontSize = GetValueProp<FontSize>(source, nameof(FontSize)),
        };

        static T? GetValueProp<T>(in ObjectSource source, string propName) where T : struct
        {
            return source.TryGetProperty(propName, out var value) ? value.Instantiate<T>() : default(T?);
        }

        static T? GetClassProp<T>(in ObjectSource source, string propName) where T : class
        {
            return source.TryGetProperty(propName, out var value) ? value.Instantiate<T>() : default(T?);
        }
    }

    protected override void ToJsonProtected(Utf8JsonWriter writer)
    {
        base.ToJsonProtected(writer);
        if(Text != null) {
            writer.WriteString(nameof(Text), Text);
        }
        if(FontSize.HasValue) {
            writer.Write(nameof(FontSize), FontSize.Value);
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

    public ButtonInfo Merged(ButtonPseudoInfo p)
    {
        return new ButtonInfo
        {
            Text = p.Text ?? Text,
            FontSize = p.FontSize ?? FontSize,
        };
    }
}

file sealed class ButtonShader : UIShader
{
    private readonly Own<Texture2D> _emptyTexture;
    private readonly Own<Sampler> _emptyTextureSampler;

    private ButtonShader(UILayer operation, ReadOnlySpan<byte> shaderSource)
        : base(shaderSource, operation, Desc)
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

    private static RenderPipelineDescriptor Desc(UILayer layer, ShaderModule module)
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
                DepthCompare = CompareFunction.LessEqual,
                Stencil = StencilState.Default,
                Bias = DepthBiasState.Default,
            },
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
        return CreateOwn(new ButtonShader(layer, sb.Utf8String));
    }

    public override Own<UIMaterial> CreateMaterial()
    {
        return ButtonShader.Material.Create(this, _emptyTexture.AsValue(), _emptyTextureSampler.AsValue()).Cast<UIMaterial>();
    }

    private sealed class Material : UIMaterial
    {
        private ButtonInfo? _buttonInfo;
        private Color4? _color;

        private Material(
            UIShader shader,
            MaybeOwn<Texture2D> texture,
            MaybeOwn<Sampler> sampler)
            : base(shader, texture, sampler)
        {
        }

        internal static Own<Material> Create(UIShader shader, MaybeOwn<Texture2D> texture, MaybeOwn<Sampler> sampler)
        {
            var self = new Material(shader, texture, sampler);
            return CreateOwn(self);
        }

        public override void UpdateMaterial(UIElement element, in LayoutCache result, in Matrix4 mvp)
        {
            base.UpdateMaterial(element, result, mvp);
            var button = (Button)element;
            Debug.Assert(button.ButtonApplied.HasValue);
            ref readonly var applied = ref button.ButtonApplied.ValueRef();
            if(_buttonInfo != applied || _color != result.AppliedInfo.Color) {
                _buttonInfo = applied;
                _color = result.AppliedInfo.Color;
                UpdateButtonTexture(applied, result.AppliedInfo.Color.ToColorByte());
            }
        }

        private void UpdateButtonTexture(in ButtonInfo button, ColorByte color)
        {
            var text = button.Text;
            using var font = new SkiaSharp.SKFont();
            font.Size = button.FontSize.Px;
            var options = new TextDrawOptions
            {
                Background = ColorByte.Transparent,
                Foreground = color,
                PowerOfTwoSizeRequired = true,
                Font = font,
            };
            TextDrawer.Draw(text, options, this, static result =>
            {
                Debug.Assert(MathTool.IsPowerOfTwo(result.Image.Size.X));
                Debug.Assert(MathTool.IsPowerOfTwo(result.Image.Size.Y));
                var material = result.Arg;
                var image = result.Image;
                if(material.Texture is Texture2D currentTex
                    && currentTex.Usage.HasFlag(TextureUsages.CopyDst)
                    && currentTex.Size == (Vector2u)image.Size) {

                    Debug.Assert(currentTex.Format == TextureFormat.Rgba8UnormSrgb);
                    Debug.Assert(currentTex.Usage.HasFlag(TextureUsages.CopyDst));
                    Debug.Assert(currentTex.MipLevelCount == 1);
                    currentTex.Write(0, image.GetPixels());
                    material.UpdateTextureContentSize(result.TextBoundsSize);
                }
                else {
                    var texture = Texture2D.CreateFromRawData(material.Shader.Screen, new()
                    {
                        Format = TextureFormat.Rgba8UnormSrgb,
                        MipLevelCount = 1,
                        SampleCount = 1,
                        Size = (Vector2u)image.Size,
                        Usage = TextureUsages.TextureBinding | TextureUsages.CopyDst,
                    }, image.GetPixels().AsBytes());
                    material.UpdateTexture(texture);
                    material.UpdateTextureContentSize(result.TextBoundsSize);
                }
            });
        }
    }
}
