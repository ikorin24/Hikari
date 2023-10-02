#nullable enable
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

    protected override void OnUpdateLayout(PseudoFlags flags, float scaleFactor)
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
    private ButtonShader(UILayer operation) : base(operation)
    {
    }

    public static Own<ButtonShader> Create(UILayer layer) => CreateOwn(new ButtonShader(layer));

    public override Own<UIMaterial> CreateMaterial()
    {
        return ButtonMaterial.Create(this, EmptyTexture, EmptySampler).Cast<UIMaterial>();
    }
}

file sealed class ButtonMaterial : UIMaterial
{
    private ButtonInfo? _buttonInfo;
    private Color4? _color;
    private float? _scaleFactor;

    private ButtonMaterial(
        UIShader shader,
        MaybeOwn<Texture2D> texture,
        MaybeOwn<Sampler> sampler)
        : base(shader, texture, sampler)
    {
    }

    internal static Own<ButtonMaterial> Create(UIShader shader, MaybeOwn<Texture2D> texture, MaybeOwn<Sampler> sampler)
    {
        var self = new ButtonMaterial(shader, texture, sampler);
        return CreateOwn(self);
    }

    public override void UpdateMaterial(UIElement element, in LayoutCache result, in Matrix4 mvp, float scaleFactor)
    {
        base.UpdateMaterial(element, result, mvp, scaleFactor);
        var button = (Button)element;
        Debug.Assert(button.ButtonApplied.HasValue);
        ref readonly var applied = ref button.ButtonApplied.ValueRef();
        if(_buttonInfo != applied || _color != result.AppliedInfo.Color || _scaleFactor != scaleFactor) {
            _buttonInfo = applied;
            _color = result.AppliedInfo.Color;
            _scaleFactor = scaleFactor;
            UpdateButtonTexture(applied, result.AppliedInfo.Color.ToColorByte(), scaleFactor);
        }
    }

    private void UpdateButtonTexture(in ButtonInfo button, ColorByte color, float scaleFactor)
    {
        var text = button.Text;
        using var font = new SkiaSharp.SKFont();
        font.Size = button.FontSize.Px * scaleFactor;
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
