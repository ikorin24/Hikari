#if HIKARI_JSON_SERDE
#nullable enable
using System.Text.Json;

namespace Hikari.UI;

partial class Label : IFromJson<Label>
{
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

    static partial void RegistorSerdeConstructor()
    {
        Serializer.RegisterConstructor(FromJson);
    }

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
}

partial record LabelPseudoInfo : IFromJson<LabelPseudoInfo>
{
    static partial void RegistorSerdeConstructor()
    {
        Serializer.RegisterConstructor(FromJson);
    }

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
}
#endif
