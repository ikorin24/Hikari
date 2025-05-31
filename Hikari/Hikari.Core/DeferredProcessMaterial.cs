#nullable enable
using Hikari.Internal;
using System;
using System.Collections.Immutable;

namespace Hikari;

public sealed class DeferredProcessMaterial : IMaterial
{
    private readonly Shader _shader;
    private ImmutableArray<BindGroupData> _pass0BindGroups;
    private EventSource<DeferredProcessMaterial> _disposed;

    public Shader Shader => _shader;

    public Screen Screen => _shader.Screen;

    public Event<DeferredProcessMaterial> Disposed => _disposed.Event;

    private DeferredProcessMaterial(Shader shader, IGBufferProvider gBuffer)
    {
        _shader = shader;
        gBuffer.Observe(gBuffer =>
        {
            var screen = gBuffer.Screen;
            _pass0BindGroups = [
                new BindGroupData(0, gBuffer.BindGroup),
                new BindGroupData(1, screen.Camera.CameraDataBindGroup),
                new BindGroupData(2, screen.Lights.DataBindGroup),
                new BindGroupData(3, screen.Lights.DirectionalLight.ShadowMapBindGroup),
            ];
        }).DisposeOn(Disposed);
    }

    public ReadOnlySpan<BindGroupData> GetBindGroups(int passIndex)
    {
        return passIndex switch
        {
            0 => _pass0BindGroups.AsSpan(),
            _ => throw new ArgumentOutOfRangeException(nameof(passIndex))
        };
    }

    private void Release()
    {
    }

    internal static Own<DeferredProcessMaterial> Create(Shader shader, IGBufferProvider gBuffer)
    {
        ArgumentNullException.ThrowIfNull(gBuffer);
        ArgumentNullException.ThrowIfNull(shader);

        var material = new DeferredProcessMaterial(shader, gBuffer);
        return Own.New(material, static x => SafeCast.As<DeferredProcessMaterial>(x).Release());
    }
}
