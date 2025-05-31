#if HIKARI_JSON_SERDE
#nullable enable
using System;
using System.Text.Json;

namespace Hikari.UI;

partial class UIElement : IToJson, IReactive
{
    protected UIElement(in ObjectSource source) : this()
    {
        if(source.TryGetProperty(nameof(Width), out var width)) {
            _info.Width = LayoutLength.FromJson(width);
        }
        if(source.TryGetProperty(nameof(Height), out var height)) {
            _info.Height = LayoutLength.FromJson(height);
        }
        if(source.TryGetProperty(nameof(Margin), out var margin)) {
            _info.Margin = Thickness.FromJson(margin);
        }
        if(source.TryGetProperty(nameof(Padding), out var padding)) {
            _info.Padding = Thickness.FromJson(padding);
        }
        if(source.TryGetProperty(nameof(HorizontalAlignment), out var horizontalAlignment)) {
            _info.HorizontalAlignment = horizontalAlignment.ToEnum<HorizontalAlignment>();
        }
        if(source.TryGetProperty(nameof(VerticalAlignment), out var verticalAlignment)) {
            _info.VerticalAlignment = verticalAlignment.ToEnum<VerticalAlignment>();
        }
        if(source.TryGetProperty(nameof(Background), out var backgroundColor)) {
            _info.Background = Brush.FromJson(backgroundColor);
        }
        if(source.TryGetProperty(nameof(BorderWidth), out var borderWidth)) {
            _info.BorderWidth = Thickness.FromJson(borderWidth);
        }
        if(source.TryGetProperty(nameof(BorderRadius), out var borderRadius)) {
            _info.BorderRadius = CornerRadius.FromJson(borderRadius);
        }
        if(source.TryGetProperty(nameof(BorderColor), out var borderColor)) {
            _info.BorderColor = Brush.FromJson(borderColor);
        }
        if(source.TryGetProperty(nameof(BoxShadow), out var boxShadow)) {
            _info.BoxShadow = BoxShadow.FromJson(boxShadow);
        }
        if(source.TryGetProperty(nameof(Flow), out var flow)) {
            _info.Flow = Flow.FromJson(flow);
        }
        if(source.TryGetProperty(nameof(Color), out var color)) {
            _info.Color = color.Instantiate<Color4>();
        }
        if(source.TryGetProperty(nameof(Clicked), out var clicked)) {
            var action = clicked.Instantiate<Action<UIElement>>();
            _clicked.Event.Subscribe(action);
        }

        if(source.TryGetProperty(nameof(Children), out var children)) {
            Children = UIElementCollection.FromJson(children);
        }
    }

    protected virtual void ToJsonProtected(Utf8JsonWriter writer)
    {
        writer.WriteString("@type", GetType().FullName);
        writer.Write(nameof(Width), _info.Width);
        writer.Write(nameof(Height), _info.Height);
        writer.Write(nameof(Margin), _info.Margin);
        writer.Write(nameof(Padding), _info.Padding);
        writer.WriteEnum(nameof(HorizontalAlignment), _info.HorizontalAlignment);
        writer.WriteEnum(nameof(VerticalAlignment), _info.VerticalAlignment);
        writer.Write(nameof(Background), _info.Background);
        writer.Write(nameof(BorderWidth), _info.BorderWidth);
        writer.Write(nameof(BorderRadius), _info.BorderRadius);
        writer.Write(nameof(BorderColor), _info.BorderColor);
        writer.Write(nameof(BoxShadow), _info.BoxShadow);
        writer.Write(nameof(Flow), _info.Flow);
        writer.Write(nameof(Color), _info.Color);
        writer.Write(nameof(Children), _children);
    }

    void IReactive.ApplyDiff(in ObjectSource source)
    {
        ApplyDiffProtected(source);
    }

    protected virtual void ApplyDiffProtected(in ObjectSource source)
    {
        Width = source.ApplyProperty(nameof(Width), Width, () => UIElementInfo.DefaultWidth, out _);
        Height = source.ApplyProperty(nameof(Height), Height, () => UIElementInfo.DefaultHeight, out _);
        Margin = source.ApplyProperty(nameof(Margin), Margin, () => UIElementInfo.DefaultMargin, out _);
        Padding = source.ApplyProperty(nameof(Padding), Padding, () => UIElementInfo.DefaultPadding, out _);
        HorizontalAlignment = source.ApplyProperty(nameof(HorizontalAlignment), HorizontalAlignment, () => UIElementInfo.DefaultHorizontalAlignment, out _);
        VerticalAlignment = source.ApplyProperty(nameof(VerticalAlignment), VerticalAlignment, () => UIElementInfo.DefaultVerticalAlignment, out _);
        Background = source.ApplyProperty(nameof(Background), Background, () => UIElementInfo.DefaultBackground, out _);
        BorderWidth = source.ApplyProperty(nameof(BorderWidth), BorderWidth, () => UIElementInfo.DefaultBorderWidth, out _);
        BorderRadius = source.ApplyProperty(nameof(BorderRadius), BorderRadius, () => UIElementInfo.DefaultBorderRadius, out _);
        BorderColor = source.ApplyProperty(nameof(BorderColor), BorderColor, () => UIElementInfo.DefaultBorderColor, out _);
        BoxShadow = source.ApplyProperty(nameof(BoxShadow), BoxShadow, () => UIElementInfo.DefaultBoxShadow, out _);
        Flow = source.ApplyProperty(nameof(Flow), Flow, () => UIElementInfo.DefaultFlow, out _);
        Color = source.ApplyProperty(nameof(Color), Color, () => UIElementInfo.DefaultColor, out _);

        if(source.TryGetProperty(nameof(Children), out var childrenProp)) {
            childrenProp.ApplyDiff(Children);
        }
    }

    public JsonValueKind ToJson(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        ToJsonProtected(writer);
        writer.WriteEndObject();
        return JsonValueKind.Object;
    }
}

partial record PseudoInfo : IToJson
{
    public JsonValueKind ToJson(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        ToJsonProtected(writer);
        writer.WriteEndObject();
        return JsonValueKind.Object;
    }

    protected virtual void ToJsonProtected(Utf8JsonWriter writer)
    {
        if(Width.HasValue) {
            writer.Write(nameof(Width), Width.Value);
        }
        if(Height.HasValue) {
            writer.Write(nameof(Height), Height.Value);
        }
        if(Margin.HasValue) {
            writer.Write(nameof(Margin), Margin.Value);
        }
        if(Padding.HasValue) {
            writer.Write(nameof(Padding), Padding.Value);
        }
        if(HorizontalAlignment.HasValue) {
            writer.WriteEnum(nameof(HorizontalAlignment), HorizontalAlignment.Value);
        }
        if(VerticalAlignment.HasValue) {
            writer.WriteEnum(nameof(VerticalAlignment), VerticalAlignment.Value);
        }
        if(Background.HasValue) {
            writer.Write(nameof(Background), Background.Value);
        }
        if(BorderWidth.HasValue) {
            writer.Write(nameof(BorderWidth), BorderWidth.Value);
        }
        if(BorderRadius.HasValue) {
            writer.Write(nameof(BorderRadius), BorderRadius.Value);
        }
        if(BorderColor.HasValue) {
            writer.Write(nameof(BorderColor), BorderColor.Value);
        }
        if(BoxShadow.HasValue) {
            writer.Write(nameof(BoxShadow), BoxShadow.Value);
        }
        if(Flow.HasValue) {
            writer.Write(nameof(Flow), Flow.Value);
        }
        if(Color.HasValue) {
            writer.Write(nameof(Color), Color.Value);
        }
    }
}
#endif
