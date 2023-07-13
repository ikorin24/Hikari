#nullable enable

namespace Elffy.UI;

public abstract class UIMaterial : Material<UIMaterial, UIShader, UILayer>
{
    public abstract BindGroup BindGroup0 { get; }
    public abstract BindGroup BindGroup1 { get; }

    protected UIMaterial(UIShader shader) : base(shader)
    {
    }

    public abstract void UpdateMaterial(UIElement element, in UIUpdateResult result);
}
