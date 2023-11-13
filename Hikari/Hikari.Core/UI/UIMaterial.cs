#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace Hikari.UI;

internal abstract class UIMaterial : Material<UIMaterial, UIShader>
{
    private readonly Own<BindGroup> _bindGroup0;
    private Own<BindGroup> _bindGroup1;
    private Own<BindGroup> _bindGroup2;
    private readonly CachedOwnBuffer<UIShaderSource.BufferData> _buffer;
    private MaterialPassData _pass0;

    private Own<Buffer> _backgroundBuffer;
    private Brush? _background;
    private MaybeOwn<Texture2D> _texture;
    private readonly MaybeOwn<Sampler> _sampler;
    private readonly Own<Buffer> _texContentSizeBuffer;
    private Vector2u? _texContentSize;

    public sealed override ReadOnlySpan<MaterialPassData> Passes => new(in _pass0);
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

        _buffer = new(screen, default, BufferUsages.Uniform | BufferUsages.CopyDst);
        _texContentSizeBuffer = Buffer.Create(Screen, (nuint)Unsafe.SizeOf<Vector2u>(), BufferUsages.Uniform | BufferUsages.CopyDst);
        _bindGroup0 = BindGroup.Create(screen, new()
        {
            Layout = shader.Passes[0].Layout.BindGroupLayouts[0],
            Entries = new[]
            {
                BindGroupEntry.Buffer(0, screen.InfoBuffer),
                BindGroupEntry.Buffer(1, _buffer),
            },
        });
        _bindGroup1 = BindGroup.Create(screen, new()
        {
            Layout = shader.Passes[0].Layout.BindGroupLayouts[1],
            Entries = new[]
            {
                BindGroupEntry.TextureView(0, texture.AsValue().View),
                BindGroupEntry.Sampler(1, sampler.AsValue()),
                BindGroupEntry.Buffer(2, _texContentSizeBuffer.AsValue()),
            },
        });
        Brush.Transparent.GetBufferData(this, static (span, self) =>
        {
            self._backgroundBuffer.Dispose();
            self._backgroundBuffer = Buffer.CreateInitSpan(self.Screen, span, BufferUsages.Uniform | BufferUsages.Storage | BufferUsages.CopyDst);
            self.SetBindGroup2();
        });
    }

    private static MaterialPassData CreatePass0(BindGroup bindGroup0, BindGroup bindGroup1, BindGroup bindGroup2)
    {
        return new MaterialPassData(0, new[]
        {
            new BindGroupData
            {
                Index = 0,
                BindGroup = bindGroup0,
            },
            new BindGroupData
            {
                Index = 1,
                BindGroup = bindGroup1,
            },
            new BindGroupData
            {
                Index = 2,
                BindGroup = bindGroup2,
            },
        });
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

    public virtual void UpdateMaterial(UIElement element, in LayoutCache result, in Matrix4 mvp, float scaleFactor)
    {
        var bufferData = new UIShaderSource.BufferData
        {
            Mvp = mvp,
            Rect = result.Layout.Rect,
            BorderWidth = result.AppliedInfo.BorderWidth.ToVector4() * scaleFactor,
            BorderRadius = result.Layout.BorderRadius,
            BorderSolidColor = result.AppliedInfo.BorderColor.SolidColor,   // TODO:
            BoxShadowValues = new()
            {
                X = result.AppliedInfo.BoxShadow.OffsetX * scaleFactor,
                Y = result.AppliedInfo.BoxShadow.OffsetY * scaleFactor,
                Z = result.AppliedInfo.BoxShadow.BlurRadius * scaleFactor,
                W = result.AppliedInfo.BoxShadow.IsInset ?
                    -result.AppliedInfo.BoxShadow.SpreadRadius * scaleFactor :
                    result.AppliedInfo.BoxShadow.SpreadRadius * scaleFactor,
            },
            BoxShadowColor = result.AppliedInfo.BoxShadow.Color,
        };
        if(_buffer.Data == bufferData) {
            _buffer.WriteData(bufferData);
        }
        UpdateBackground(result.AppliedInfo.Background);
    }

    internal void UpdateTexture(MaybeOwn<Texture2D> texture)
    {
        var textureValue = texture.AsValue();
        _texture.Dispose();
        _texture = texture;
        _bindGroup1.Dispose();
        _bindGroup1 = BindGroup.Create(Screen, new()
        {
            Layout = Shader.Passes[0].Layout.BindGroupLayouts[1],
            Entries = new[]
            {
                BindGroupEntry.TextureView(0, textureValue.View),
                BindGroupEntry.Sampler(1, _sampler.AsValue()),
                BindGroupEntry.Buffer(2, _texContentSizeBuffer.AsValue()),
            },
        });
        _pass0 = CreatePass0(_bindGroup0.AsValue(), _bindGroup1.AsValue(), _bindGroup2.AsValue());
    }

    internal void UpdateTextureContentSize(Vector2u contentSize)
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
            Layout = Shader.Passes[0].Layout.BindGroupLayouts[2],
            Entries = new[]
            {
                BindGroupEntry.Buffer(0, _backgroundBuffer.AsValue()),
            },
        });
        _pass0 = CreatePass0(_bindGroup0.AsValue(), _bindGroup1.AsValue(), _bindGroup2.AsValue());
    }
}
