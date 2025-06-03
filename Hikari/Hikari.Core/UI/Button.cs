#nullable enable
using System;
using System.Diagnostics;

namespace Hikari.UI;

public sealed partial class Button : UIElement
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
        RegistorSerdeConstructor();
        UITree.RegisterMaterial<Button>(static screen =>
        {
            return ButtonMaterial.Create(UIShader.CreateOrCached(screen)).Cast<IUIMaterial>();
        });

    }

    static partial void RegistorSerdeConstructor();

    public Button() : base()
    {
        _info = ButtonInfo.Default;
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

public sealed partial record ButtonPseudoInfo : PseudoInfo
{
    static ButtonPseudoInfo() => RegistorSerdeConstructor();

    static partial void RegistorSerdeConstructor();

    public string? Text { get; init; }
    public FontSize? FontSize { get; init; }
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

file sealed class ButtonMaterial : IUIMaterial
{
    private ButtonInfo? _buttonInfo;
    private Color4? _color;
    private float? _scaleFactor;
    private UIMaterialBase _base;

    public Screen Screen => _base.Screen;

    public UIShader Shader => _base.Shader;
    ITypedShader IMaterial.Shader => Shader;

    private ButtonMaterial(UIShader shader)
    {
        _base = new UIMaterialBase(shader);
    }

    private void Release()
    {
        _base.Release();
    }

    internal static Own<ButtonMaterial> Create(UIShader shader)
    {
        var self = new ButtonMaterial(shader);
        return Own.New(self, static x => SafeCast.As<ButtonMaterial>(x).Release());
    }

    public void UpdateMaterial(UIElement element, in LayoutCache result, in Matrix4 mvp, float scaleFactor)
    {
        _base.UpdateMaterial(element, result, mvp, scaleFactor);
        var button = (Button)element;
        Debug.Assert(button.ButtonApplied.HasValue);
        ref readonly var applied = ref button.ButtonApplied.ValueRef();
        if(_buttonInfo != applied || _color != result.AppliedInfo.Color || _scaleFactor != scaleFactor) {
            _buttonInfo = applied;
            _color = result.AppliedInfo.Color;
            _scaleFactor = scaleFactor;
            var (newTexture, contentSize, changed) = TextMaterialHelper.UpdateTextTexture(Screen, _base.Texture, applied.Text, applied.FontSize, result.AppliedInfo.Color.ToColorByte(), scaleFactor);
            if(changed) {
                _base.UpdateTexture(newTexture);
            }
            _base.UpdateTextureContentSize(contentSize);
        }
    }

    public void SetBindGroupsTo(in RenderPass renderPass, int passIndex, Renderer renderer)
    {
        _base.SetBindGroupsTo(renderPass, passIndex, renderer);
    }
}
