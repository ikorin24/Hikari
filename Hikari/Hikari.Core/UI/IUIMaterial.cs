#nullable enable
using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Hikari.UI;

internal interface IUIMaterial : IMaterial
{
    void UpdateMaterial(UIElement element, in LayoutCache result, in Matrix4 mvp, float scaleFactor);
}

internal struct UIMaterialBase
{
    private readonly Shader _shader;
    private readonly Own<BindGroup> _bindGroup0;
    private Own<BindGroup> _bindGroup1;
    private Own<BindGroup> _bindGroup2;
    private readonly CachedOwnBuffer<UIShaderSource.BufferData> _buffer;
    private ImmutableArray<BindGroupData> _pass0Bindgroups;

    private Own<Buffer> _backgroundBuffer;
    private Brush? _background;
    private MaybeOwn<Texture2D> _texture;
    private readonly MaybeOwn<Sampler> _sampler;
    private readonly Own<Buffer> _texContentSizeBuffer;
    private Vector2u? _texContentSize;

    private const BufferUsages BackgroundBufferUsage = BufferUsages.Uniform | BufferUsages.Storage | BufferUsages.CopyDst;

    public readonly Shader Shader => _shader;
    public readonly Screen Screen => _shader.Screen;
    public readonly Texture2D? Texture => _texture.TryAsValue(out var texture) ? texture : null;

    public readonly bool IsManaged => true;  // TODO:

    public UIMaterialBase(Shader shader) : this(shader, UIShader.GetEmptyTexture2D(shader.Screen), UIShader.GetEmptySampler(shader.Screen))
    {
    }

    public UIMaterialBase(Shader shader, MaybeOwn<Texture2D> texture, MaybeOwn<Sampler> sampler)
    {
        texture.ThrowArgumentExceptionIfNone(nameof(texture));
        sampler.ThrowArgumentExceptionIfNone(nameof(sampler));
        _shader = shader;
        var screen = shader.Screen;
        _texture = texture;
        _sampler = sampler;

        var passes = shader.ShaderPasses;
        _buffer = new(screen, default, BufferUsages.Uniform | BufferUsages.CopyDst);
        _texContentSizeBuffer = Buffer.Create(screen, (nuint)Unsafe.SizeOf<Vector2u>(), BufferUsages.Uniform | BufferUsages.CopyDst);
        _bindGroup0 = BindGroup.Create(screen, new()
        {
            Layout = passes[0].Pipeline.Layout.BindGroupLayouts[0],
            Entries =
            [
                BindGroupEntry.Buffer(0, screen.InfoBuffer),
                BindGroupEntry.Buffer(1, _buffer),
            ],
        });
        _bindGroup1 = BindGroup.Create(screen, new()
        {
            Layout = passes[0].Pipeline.Layout.BindGroupLayouts[1],
            Entries =
            [
                BindGroupEntry.TextureView(0, texture.AsValue().View),
                BindGroupEntry.Sampler(1, sampler.AsValue()),
                BindGroupEntry.Buffer(2, _texContentSizeBuffer.AsValue()),
            ],
        });
        _backgroundBuffer.Dispose();
        _backgroundBuffer = Brush.Transparent.GetBufferData(this, static (span, self) => Buffer.Create(self.Screen, span, BackgroundBufferUsage));
        SetBindGroup2();
    }

    internal readonly void Release()
    {
        _bindGroup0.Dispose();
        _bindGroup1.Dispose();
        _bindGroup2.Dispose();
        _buffer.Dispose();
        _backgroundBuffer.Dispose();
        _texture.Dispose();
        _sampler.Dispose();
        _texContentSizeBuffer.Dispose();
    }

    public readonly ReadOnlySpan<BindGroupData> GetBindGroups(int passIndex)
    {
        return passIndex switch
        {
            0 => _pass0Bindgroups.AsSpan(),
            _ => throw new ArgumentOutOfRangeException(nameof(passIndex)),
        };
    }

    public void Validate()
    {
        _texture.Validate();
        _sampler.Validate();
    }

    public void UpdateMaterial(UIElement element, in LayoutCache result, in Matrix4 mvp, float scaleFactor)
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
        if(_buffer.Data != bufferData) {
            _buffer.WriteData(bufferData);
        }
        UpdateBackground(result.AppliedInfo.Background);
    }

    internal void UpdateTexture(MaybeOwn<Texture2D> texture)
    {
        var pass0 = Shader.ShaderPasses[0];
        var textureValue = texture.AsValue();
        _texture.Dispose();
        _texture = texture;
        _bindGroup1.Dispose();
        _bindGroup1 = BindGroup.Create(Screen, new()
        {
            Layout = pass0.Pipeline.Layout.BindGroupLayouts[1],
            Entries =
            [
                BindGroupEntry.TextureView(0, textureValue.View),
                BindGroupEntry.Sampler(1, _sampler.AsValue()),
                BindGroupEntry.Buffer(2, _texContentSizeBuffer.AsValue()),
            ],
        });
        _pass0Bindgroups = [
            new(0, _bindGroup0.AsValue()),
            new(1, _bindGroup1.AsValue()),
            new(2, _bindGroup2.AsValue()),
        ];
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
                _backgroundBuffer.Dispose();
                _backgroundBuffer = background.GetBufferData(this, static (span, self) => Buffer.Create(self.Screen, span, BackgroundBufferUsage));
                SetBindGroup2();
            }
            _background = background;
        }
    }

    private void SetBindGroup2()
    {
        var pass0 = Shader.ShaderPasses[0];
        _bindGroup2.Dispose();
        _bindGroup2 = BindGroup.Create(Screen, new()
        {
            Layout = pass0.Pipeline.Layout.BindGroupLayouts[2],
            Entries =
            [
                BindGroupEntry.Buffer(0, _backgroundBuffer.AsValue()),
            ],
        });
        _pass0Bindgroups = [
            new(0, _bindGroup0.AsValue()),
            new(1, _bindGroup1.AsValue()),
            new(2, _bindGroup2.AsValue()),
        ];
    }
}
