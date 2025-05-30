#nullable enable
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace Hikari.UI;

public sealed class Label : UIElement
#if HIKARI_JSON_SERDE
    , IFromJson<Label>
#endif
{
    private LabelInfo _info;
    private LabelPseudoInfo? _hoverInfo;
    private LabelPseudoInfo? _activeInfo;
    private LabelInfo? _appliedInfo;

    internal ref readonly LabelInfo? LabelApplied => ref _appliedInfo;

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

    public LabelPseudoInfo? HoverProps
    {
        get => GetHoverProps();
        set
        {
            if(_hoverInfo == value) { return; }
            _hoverInfo = value;
            RequestRelayout();
        }
    }

    public LabelPseudoInfo? ActiveProps
    {
        get => _activeInfo;
        set
        {
            if(_activeInfo == value) { return; }
            _activeInfo = value;
            RequestRelayout();
        }
    }

    protected override LabelPseudoInfo? GetHoverProps() => _hoverInfo;

    protected override LabelPseudoInfo? GetActiveProps() => _activeInfo;

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

    static Label()
    {
#if HIKARI_JSON_SERDE
        Serializer.RegisterConstructor(FromJson);
#endif
        UITree.RegisterMaterial<Label>(static screen =>
        {
            return LabelMaterial.Create(UIShader.CreateOrCached(screen)).Cast<IUIMaterial>();
        });
    }

#if HIKARI_JSON_SERDE
    public static Label FromJson(in ObjectSource source) => new Label(source);

    protected override void ToJsonProtected(Utf8JsonWriter writer)
    {
        base.ToJsonProtected(writer);
        writer.WriteString(nameof(Text), Text);
        writer.Write(nameof(FontSize), FontSize);
    }

    protected override void ApplyDiffProtected(in ObjectSource source)
    {
        base.ApplyDiffProtected(source);
        Text = source.ApplyProperty(nameof(Text), Text, () => LabelInfo.DefaultText, out _);
        FontSize = source.ApplyProperty(nameof(FontSize), FontSize, () => LabelInfo.DefaultFontSize, out _);
    }
#endif

    public Label() : base()
    {
        _info = LabelInfo.Default;
    }

#if HIKARI_JSON_SERDE
    private Label(in ObjectSource source) : base(source)
    {
        _info = LabelInfo.Default;
        if(source.TryGetProperty(nameof(Text), out var text)) {
            Text = Serializer.Instantiate<string>(text);
        }
        if(source.TryGetProperty(nameof(FontSize), out var fontSize)) {
            FontSize = Serializer.Instantiate<FontSize>(fontSize);
        }
        if(source.TryGetProperty(PseudoInfo.HoverName, out var hover)) {
            _hoverInfo = LabelPseudoInfo.FromJson(hover);
        }
        if(source.TryGetProperty(PseudoInfo.ActiveName, out var active)) {
            _activeInfo = LabelPseudoInfo.FromJson(active);
        }
    }
#endif
}

public sealed record LabelPseudoInfo
    : PseudoInfo
#if HIKARI_JSON_SERDE
    , IFromJson<LabelPseudoInfo>
#endif
{
#if HIKARI_JSON_SERDE
    static LabelPseudoInfo() => Serializer.RegisterConstructor(FromJson);
#endif

    public string? Text { get; init; }
    public FontSize? FontSize { get; init; }

#if HIKARI_JSON_SERDE

    public static LabelPseudoInfo FromJson(in ObjectSource source)
    {
        return new LabelPseudoInfo
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
#endif
}

internal record struct LabelInfo
{
    public required string Text { get; set; }
    public required FontSize FontSize { get; set; }

    public static LabelInfo Default => new()
    {
        Text = DefaultText,
        FontSize = DefaultFontSize,
    };

    public static string DefaultText => "";
    public static FontSize DefaultFontSize => 16;

    public LabelInfo Merged(LabelPseudoInfo p)
    {
        return new LabelInfo
        {
            Text = p.Text ?? Text,
            FontSize = p.FontSize ?? FontSize,
        };
    }
}

file sealed class LabelMaterial : IUIMaterial
{
    private LabelInfo? _labelInfo;
    private Color4? _color;
    private float? _scaleFactor;
    private UIMaterialBase _base;

    private LabelMaterial(Shader shader)
    {
        _base = new UIMaterialBase(shader);
    }

    private void Release()
    {
        _base.Release();
    }

    public Screen Screen => _base.Screen;

    public Shader Shader => _base.Shader;

    internal static Own<LabelMaterial> Create(Shader shader)
    {
        var self = new LabelMaterial(shader);
        return Own.New(self, static x => SafeCast.As<LabelMaterial>(x).Release());
    }

    public ReadOnlySpan<BindGroupData> GetBindGroups(int passIndex) => _base.GetBindGroups(passIndex);

    public void UpdateMaterial(UIElement element, in LayoutCache result, in Matrix4 mvp, float scaleFactor)
    {
        _base.UpdateMaterial(element, result, mvp, scaleFactor);
        var label = (Label)element;
        Debug.Assert(label.LabelApplied.HasValue);
        ref readonly var applied = ref label.LabelApplied.ValueRef();
        if(_labelInfo != applied || _color != result.AppliedInfo.Color || _scaleFactor != scaleFactor) {
            _labelInfo = applied;
            _color = result.AppliedInfo.Color;
            _scaleFactor = scaleFactor;
            var (newTexture, contentSize, changed) = TextMaterialHelper.UpdateTextTexture(Screen, _base.Texture, applied.Text, applied.FontSize, result.AppliedInfo.Color.ToColorByte(), scaleFactor);
            if(changed) {
                _base.UpdateTexture(newTexture);
            }
            _base.UpdateTextureContentSize(contentSize);

        }
    }
}
