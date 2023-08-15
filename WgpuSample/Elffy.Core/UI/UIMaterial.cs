#nullable enable

namespace Elffy.UI;

internal abstract class UIMaterial : Material<UIMaterial, UIShader, UILayer>
{
    private Own<Buffer> _backgroundBrushBuffer;
    private Brush? _background;

    public abstract BindGroup BindGroup0 { get; }
    public abstract BindGroup BindGroup1 { get; }

    protected UIMaterial(UIShader shader) : base(shader)
    {
        _backgroundBrushBuffer = Own<Buffer>.None;
    }

    protected override void Release(bool manualRelease)
    {
        base.Release(manualRelease);
        if(manualRelease) {
            _backgroundBrushBuffer.Dispose();
        }
    }

    public abstract void UpdateMaterial(UIElement element, in UIUpdateResult result);

    protected void UpdateBackground(in Brush background)
    {
        if(_background != background) {
            if(_background != null && _background.Value.GetBufferDataSize() == background.GetBufferDataSize()) {
                background.GetBufferData(
                    _backgroundBrushBuffer.AsValue(),
                    static (span, buffer) => buffer.WriteSpan(0, span));
            }
            else {
                background.GetBufferData(this, static (span, self) =>
                {
                    self._backgroundBrushBuffer.Dispose();
                    self._backgroundBrushBuffer = Buffer.CreateInitSpan(self.Screen, span, BufferUsages.Uniform | BufferUsages.CopyDst);
                });
            }
            _background = background;
        }
    }
}
