#nullable enable
using System;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Hikari.UI;

public sealed class Panel : UIElement
#if HIKARI_JSON_SERDE
    , IFromJson<Panel>
#endif
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
#if HIKARI_JSON_SERDE
        Serializer.RegisterConstructor(FromJson);
#endif
        UITree.RegisterMaterial<Panel>(static screen =>
        {
            return PanelMaterial.Create(UIShader.CreateOrCached(screen)).Cast<IUIMaterial>();
        });

    }

    public Panel()
    {
    }

#if HIKARI_JSON_SERDE
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
#endif

    protected override PanelPseudoInfo? GetHoverProps() => _hoverInfo;

    protected override PanelPseudoInfo? GetActiveProps() => _activeInfo;

    protected override void OnUpdateLayout(PseudoFlags flags, float scaleFactor)
    {
        // nop
    }
}

public sealed record PanelPseudoInfo
    : PseudoInfo
#if HIKARI_JSON_SERDE
    , IFromJson<PanelPseudoInfo>
#endif
{
#if HIKARI_JSON_SERDE
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
#endif
}

file sealed class PanelMaterial : IUIMaterial
{
    private UIMaterialBase _base;

    private PanelMaterial(Shader shader)
    {
        _base = new UIMaterialBase(shader);
    }

    public Screen Screen => _base.Screen;

    public Shader Shader => _base.Shader;

    internal static Own<PanelMaterial> Create(Shader shader)
    {
        var self = new PanelMaterial(shader);
        return Own.New(self, static x => SafeCast.As<PanelMaterial>(x).Release());
    }

    private void Release()
    {
        _base.Release();
    }

    public ReadOnlySpan<BindGroupData> GetBindGroups(int passIndex) => _base.GetBindGroups(passIndex);

    public void UpdateMaterial(UIElement element, in LayoutCache result, in Matrix4 mvp, float scaleFactor)
    {
        _base.UpdateMaterial(element, result, mvp, scaleFactor);
    }
}
