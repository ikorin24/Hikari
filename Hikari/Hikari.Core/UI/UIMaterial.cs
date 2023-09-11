#nullable enable
using System.Runtime.CompilerServices;

namespace Hikari.UI;

internal abstract class UIMaterial : Material<UIMaterial, UIShader, UILayer>
{
    private readonly Own<BindGroup> _bindGroup0;
    private Own<BindGroup> _bindGroup1;
    private Own<BindGroup> _bindGroup2;
    private readonly Own<Buffer> _buffer;
    private UIShaderSource.BufferData? _bufferData;
    private Own<Buffer> _backgroundBuffer;
    private Brush? _background;
    private MaybeOwn<Texture2D> _texture;
    private readonly MaybeOwn<Sampler> _sampler;
    private readonly Own<Buffer> _texContentSizeBuffer;
    private Vector2u? _texContentSize;

    public BindGroup BindGroup0 => _bindGroup0.AsValue();
    public BindGroup BindGroup1 => _bindGroup1.AsValue();
    public BindGroup BindGroup2 => _bindGroup2.AsValue();

    protected Buffer DataBuffer => _buffer.AsValue();
    protected Buffer BackgroundBuffer => _backgroundBuffer.AsValue();

    public Texture2D? Texture => _texture.TryAsValue(out var texture) ? texture : null;

    protected UIMaterial(
        UIShader shader,
        MaybeOwn<Texture2D> texture,
        MaybeOwn<Sampler> sampler) : base(shader)
    {
        texture.ThrowArgumentExceptionIfNone(nameof(texture));
        sampler.ThrowArgumentExceptionIfNone(nameof(sampler));
        var screen = Screen;
        _texture = texture;
        _sampler = sampler;

        _buffer = Buffer.Create(Screen, (nuint)Unsafe.SizeOf<UIShaderSource.BufferData>(), BufferUsages.Uniform | BufferUsages.CopyDst);
        _texContentSizeBuffer = Buffer.Create(Screen, (nuint)Unsafe.SizeOf<Vector2u>(), BufferUsages.Uniform | BufferUsages.CopyDst);
        _bindGroup0 = BindGroup.Create(screen, new()
        {
            Layout = Operation.BindGroupLayout0,
            Entries = new[]
            {
                BindGroupEntry.Buffer(0, screen.InfoBuffer),
                BindGroupEntry.Buffer(1, _buffer.AsValue()),
            },
        });
        _bindGroup1 = BindGroup.Create(screen, new()
        {
            Layout = Operation.BindGroupLayout1,
            Entries = new[]
            {
                BindGroupEntry.TextureView(0, texture.AsValue().View),
                BindGroupEntry.Sampler(1, sampler.AsValue()),
                BindGroupEntry.Buffer(2, _texContentSizeBuffer.AsValue()),
            },
        });
        _backgroundBuffer = Own<Buffer>.None;
    }

    protected override void Release(bool manualRelease)
    {
        base.Release(manualRelease);
        if(manualRelease) {
            _bindGroup0.Dispose();
            _bindGroup1.Dispose();
            _bindGroup2.Dispose();
            _buffer.Dispose();
            _backgroundBuffer.Dispose();
            _texture.Dispose();
            _sampler.Dispose();
            _texContentSizeBuffer.Dispose();
        }
    }

    public override void Validate()
    {
        base.Validate();
        _texture.Validate();
        _sampler.Validate();
    }

    public virtual void UpdateMaterial(UIElement element, in LayoutCache result, in Matrix4 mvp)
    {
        var bufferData = new UIShaderSource.BufferData
        {
            Mvp = mvp,
            Rect = result.Layout.Rect,
            BorderWidth = result.AppliedInfo.BorderWidth.ToVector4(),
            BorderRadius = result.Layout.BorderRadius,
            BorderSolidColor = result.AppliedInfo.BorderColor.SolidColor,   // TODO:
            BoxShadowValues = new()
            {
                X = result.AppliedInfo.BoxShadow.OffsetX,
                Y = result.AppliedInfo.BoxShadow.OffsetY,
                Z = result.AppliedInfo.BoxShadow.BlurRadius,
                W = result.AppliedInfo.BoxShadow.IsInset ? -result.AppliedInfo.BoxShadow.SpreadRadius : result.AppliedInfo.BoxShadow.SpreadRadius,
            },
            BoxShadowColor = result.AppliedInfo.BoxShadow.Color,
        };
        if(_bufferData != bufferData) {
            _bufferData = bufferData;
            _buffer.AsValue().WriteData(0, bufferData);
        }
        UpdateBackground(result.AppliedInfo.Background);
    }

    protected void UpdateTexture(MaybeOwn<Texture2D> texture)
    {
        var textureValue = texture.AsValue();
        _texture.Dispose();
        _texture = texture;
        _bindGroup1.Dispose();
        _bindGroup1 = BindGroup.Create(Screen, new()
        {
            Layout = Operation.BindGroupLayout1,
            Entries = new[]
            {
                BindGroupEntry.TextureView(0, textureValue.View),
                BindGroupEntry.Sampler(1, _sampler.AsValue()),
                BindGroupEntry.Buffer(2, _texContentSizeBuffer.AsValue()),
            },
        });
    }

    protected void UpdateTextureContentSize(Vector2u contentSize)
    {
        if(_texContentSize != contentSize) {
            _texContentSizeBuffer.AsValue().WriteData(0, contentSize);
            _texContentSize = contentSize;
        }
    }

    private void UpdateBackground(in Brush background)
    {
        if(_background != background) {
            if(_background != null && _background.Value.GetBufferDataSize() == background.GetBufferDataSize()) {
                background.GetBufferData(
                    _backgroundBuffer.AsValue(),
                    static (span, buffer) => buffer.WriteSpan(0, span));
            }
            else {
                background.GetBufferData(this, static (span, self) =>
                {
                    self._backgroundBuffer.Dispose();
                    self._backgroundBuffer = Buffer.CreateInitSpan(self.Screen, span, BufferUsages.Uniform | BufferUsages.Storage | BufferUsages.CopyDst);
                    self.SetBindGroup2();
                });
            }
            _background = background;
        }
    }

    private void SetBindGroup2()
    {
        _bindGroup2.Dispose();
        _bindGroup2 = BindGroup.Create(Screen, new()
        {
            Layout = Operation.BindGroupLayout2,
            Entries = new[]
            {
                BindGroupEntry.Buffer(0, _backgroundBuffer.AsValue()),
            },
        });
    }
}
