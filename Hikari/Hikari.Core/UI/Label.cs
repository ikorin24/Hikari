#nullable enable
using System;
using System.Diagnostics;

namespace Hikari.UI;

public sealed partial class Label : UIElement
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

    public TextAlignment TextAlignment
    {
        get => _info.TextAlignment;
        set
        {
            if(value == _info.TextAlignment) { return; }
            _info.TextAlignment = value;
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
        RegistorSerdeConstructor();
        UITree.RegisterMaterial<Label>(static screen =>
        {
            return LabelMaterial.Create(UIShader.CreateOrCached(screen)).Cast<IUIMaterial>();
        });
    }

    static partial void RegistorSerdeConstructor();

    public Label() : base()
    {
        _info = LabelInfo.Default;
    }
}

public sealed partial record LabelPseudoInfo : PseudoInfo
{
    static LabelPseudoInfo() => RegistorSerdeConstructor();

    static partial void RegistorSerdeConstructor();

    public string? Text { get; init; }
    public FontSize? FontSize { get; init; }
    public TextAlignment? TextAlignment { get; init; }
}

internal record struct LabelInfo
{
    public required string Text { get; set; }
    public required FontSize FontSize { get; set; }
    public required TextAlignment TextAlignment { get; set; }

    public static LabelInfo Default => new()
    {
        Text = DefaultText,
        FontSize = DefaultFontSize,
        TextAlignment = DefaultTextAlignment,
    };

    public static string DefaultText => "";
    public static FontSize DefaultFontSize => 16;
    public static TextAlignment DefaultTextAlignment => TextAlignment.Center;

    public LabelInfo Merged(LabelPseudoInfo p)
    {
        return new LabelInfo
        {
            Text = p.Text ?? Text,
            FontSize = p.FontSize ?? FontSize,
            TextAlignment = p.TextAlignment ?? TextAlignment,
        };
    }
}

file sealed class LabelMaterial : IUIMaterial
{
    private LabelInfo? _labelInfo;
    private Color4? _color;
    private float? _scaleFactor;
    private UIMaterialBase _base;

    private LabelMaterial(UIShader shader)
    {
        _base = new UIMaterialBase(shader);
    }

    private void Release()
    {
        _base.Release();
    }

    public Screen Screen => _base.Screen;

    public UIShader Shader => _base.Shader;

    ITypedShader IMaterial.Shader => Shader;

    internal static Own<LabelMaterial> Create(UIShader shader)
    {
        var self = new LabelMaterial(shader);
        return Own.New(self, static x => SafeCast.As<LabelMaterial>(x).Release());
    }

    public void SetBindGroupsTo(in RenderPass renderPass, int passIndex, Renderer renderer)
    {
        _base.SetBindGroupsTo(renderPass, passIndex, renderer);
    }

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
            var arg = new UpdateTextTextureArg
            {
                Text = applied.Text,
                FontSize = applied.FontSize,
                Color = result.AppliedInfo.Color.ToColorByte(),
                ScaleFactor = scaleFactor,
                TextAlignment = applied.TextAlignment,
            };
            var (newTexture, contentSize, changed) = TextMaterialHelper.UpdateTextTexture(Screen, _base.Texture, arg);
            if(changed) {
                _base.UpdateTexture(newTexture);
            }
            _base.UpdateTextureContentSize(contentSize);

        }
    }
}
