#nullable enable
using System;

namespace Hikari.UI;

public sealed partial class Panel : UIElement
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
        RegistorSerdeConstructor();
        UITree.RegisterMaterial<Panel>(static screen =>
        {
            return PanelMaterial.Create(UIShader.CreateOrCached(screen)).Cast<IUIMaterial>();
        });
    }

    static partial void RegistorSerdeConstructor();

    public Panel()
    {
    }

    protected override PanelPseudoInfo? GetHoverProps() => _hoverInfo;

    protected override PanelPseudoInfo? GetActiveProps() => _activeInfo;

    protected override void OnUpdateLayout(PseudoFlags flags, float scaleFactor)
    {
        // nop
    }
}

public sealed partial record PanelPseudoInfo : PseudoInfo
{
    static PanelPseudoInfo() => RegistorSerdeConstructor();

    static partial void RegistorSerdeConstructor();
}

file sealed class PanelMaterial : IUIMaterial
{
    private UIMaterialBase _base;

    private PanelMaterial(UIShader shader)
    {
        _base = new UIMaterialBase(shader);
    }

    public Screen Screen => _base.Screen;

    public UIShader Shader => _base.Shader;

    ITypedShader IMaterial.Shader => Shader;

    internal static Own<PanelMaterial> Create(UIShader shader)
    {
        var self = new PanelMaterial(shader);
        return Own.New(self, static x => SafeCast.As<PanelMaterial>(x).Release());
    }

    private void Release()
    {
        _base.Release();
    }

    public void SetBindGroupsTo(in RenderPass renderPass, int passIndex, Renderer renderer)
    {
        _base.SetBindGroupsTo(in renderPass, passIndex, renderer);
    }

    public void UpdateMaterial(UIElement element, in LayoutCache result, in Matrix4 mvp, float scaleFactor)
    {
        _base.UpdateMaterial(element, result, mvp, scaleFactor);
    }
}
