#nullable enable
using System;
using System.Collections.Concurrent;

namespace Hikari.UI;

internal sealed class UIModel : FrameObject
{
    private readonly UIElement _element;

    internal UIElement Element => _element;

    internal UIModel(UIElement element, UIShader shader)
        : base(
            GetMesh(shader.Screen),
            [shader.CreateMaterial().Cast<Material>()])
    {
        _element = element;
    }

    protected override void PrepareForRender()
    {
        var screen = Screen;
        var screenSize = screen.ClientSize;
        var scaleFactor = screen.ScaleFactor;
        var uiProjection = Matrix4.ReversedZ.OrthographicProjection(0, (float)screenSize.X, 0, (float)screenSize.Y, 0, 1f);
        _element.UpdateMaterial(screenSize, scaleFactor, uiProjection, 0);
    }

    private static Mesh GetMesh(Screen screen)
    {
        return _cache.GetOrAdd(screen, static screen =>
        {
            ReadOnlySpan<VertexSlim> vertices =
            [
                new VertexSlim(0, 1, 0, 0, 0),
                new VertexSlim(0, 0, 0, 0, 1),
                new VertexSlim(1, 0, 0, 1, 1),
                new VertexSlim(1, 1, 0, 1, 0),
            ];
            ReadOnlySpan<ushort> indices = [0, 1, 2, 2, 3, 0];
            screen.Closed.Subscribe(static screen =>
            {
                if(_cache.TryRemove(screen, out var cache)) {
                    cache.Dispose();
                }
            });
            return Mesh.Create<VertexSlim, ushort>(screen, vertices, indices);
        }).AsValue();
    }

    private static readonly ConcurrentDictionary<Screen, Own<Mesh>> _cache = new();
}
