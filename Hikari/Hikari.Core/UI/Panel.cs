#nullable enable
using System.Text.Json;

namespace Hikari.UI;

public sealed class Panel : UIElement, IFromJson<Panel>
{
    private PanelPseudoInfo? _hoverInfo;
    private PanelPseudoInfo? _activeInfo;

    public PanelPseudoInfo? HoverProps
    {
        get => GetHoverProps();
        set
        {
            if(_hoverInfo == value) { return; }
            _hoverInfo = value;
            RequestRelayout();
        }
    }

    public PanelPseudoInfo? ActiveProps
    {
        get => _activeInfo;
        set
        {
            if(_activeInfo == value) { return; }
            _activeInfo = value;
            RequestRelayout();
        }
    }

    static Panel()
    {
        Serializer.RegisterConstructor(FromJson);
        UILayer.RegisterShader<Panel>(static layer =>
        {
            return PanelShader.Create(layer).Cast<UIShader>();
        });
    }

    public static Panel FromJson(in ObjectSource source) => new Panel(source);

    public Panel()
    {
    }

    private Panel(in ObjectSource source) : base(source)
    {
    }

    protected override void ToJsonProtected(Utf8JsonWriter writer)
    {
        base.ToJsonProtected(writer);
    }

    protected override void ApplyDiffProtected(in ObjectSource source)
    {
        base.ApplyDiffProtected(source);
    }

    protected override PanelPseudoInfo? GetHoverProps() => _hoverInfo;

    protected override PanelPseudoInfo? GetActiveProps() => _activeInfo;

    protected override void OnUpdateLayout(PseudoFlags flags, float scaleFactor)
    {
        // nop
    }
}

public sealed record PanelPseudoInfo
    : PseudoInfo,
    IFromJson<PanelPseudoInfo>
{
    static PanelPseudoInfo() => Serializer.RegisterConstructor(FromJson);

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

file sealed class PanelShader : UIShader
{
    private PanelShader(UILayer operation) : base(operation)
    {
    }

    public static Own<PanelShader> Create(UILayer layer)
    {
        return CreateOwn(new PanelShader(layer));
    }

    public override Own<UIMaterial> CreateMaterial()
    {
        return PanelMaterial.Create(this, EmptyTexture, EmptySampler).Cast<UIMaterial>();
    }
}

file sealed class PanelMaterial : UIMaterial
{
    private PanelMaterial(
        UIShader shader,
        MaybeOwn<Texture2D> texture,
        MaybeOwn<Sampler> sampler)
        : base(shader, texture, sampler)
    {
    }

    internal static Own<PanelMaterial> Create(UIShader shader, MaybeOwn<Texture2D> texture, MaybeOwn<Sampler> sampler)
    {
        var self = new PanelMaterial(shader, texture, sampler);
        return CreateOwn(self);
    }

    public override void UpdateMaterial(UIElement element, in LayoutCache result, in Matrix4 mvp, float scaleFactor)
    {
        base.UpdateMaterial(element, result, mvp, scaleFactor);
    }
}
