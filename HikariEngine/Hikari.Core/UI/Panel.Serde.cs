#if HIKARI_JSON_SERDE
#nullable enable
using System.Text.Json;

namespace Hikari.UI;

partial class Panel : IFromJson<Panel>
{
    static partial void RegistorSerdeConstructor()
    {
        Serializer.RegisterConstructor(FromJson);
    }
    private Panel(in ObjectSource source) : base(source)
    {
    }

    public static Panel FromJson(in ObjectSource source) => new Panel(source);

    protected override void ToJsonProtected(Utf8JsonWriter writer)
    {
        base.ToJsonProtected(writer);
    }

    protected override void ApplyDiffProtected(in ObjectSource source)
    {
        base.ApplyDiffProtected(source);
    }
}

partial record PanelPseudoInfo : IFromJson<PanelPseudoInfo>
{
    static partial void RegistorSerdeConstructor()
    {
        Serializer.RegisterConstructor(FromJson);
    }
    public static PanelPseudoInfo FromJson(in ObjectSource source)
    {
        return new()
        {
            Width = Get<LayoutLength>(source, nameof(Width)),
            Height = Get<LayoutLength>(source, nameof(Height)),
            Margin = Get<Thickness>(source, nameof(Margin)),
            Padding = Get<Thickness>(source, nameof(Padding)),
            HorizontalAlignment = Get<HorizontalAlignment>(source, nameof(HorizontalAlignment)),
            VerticalAlignment = Get<VerticalAlignment>(source, nameof(VerticalAlignment)),
            Background = Get<Brush>(source, nameof(Background)),
            BorderWidth = Get<Thickness>(source, nameof(BorderWidth)),
            BorderRadius = Get<CornerRadius>(source, nameof(BorderRadius)),
            BorderColor = Get<Brush>(source, nameof(BorderColor)),
            BoxShadow = Get<BoxShadow>(source, nameof(BoxShadow)),
            Flow = Get<Flow>(source, nameof(Flow)),
            Color = Get<Color4>(source, nameof(Color)),
        };

        static T? Get<T>(in ObjectSource source, string propName) where T : struct
        {
            return source.TryGetProperty(propName, out var value) ? value.Instantiate<T>() : default(T?);
        }
    }
}
#endif
