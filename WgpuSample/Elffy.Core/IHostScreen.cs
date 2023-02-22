#nullable enable
using Elffy.NativeBind;
using System;
using System.Runtime.CompilerServices;

namespace Elffy;

public interface IHostScreen
{
    event RedrawRequestedAction? RedrawRequested;
    event Action<IHostScreen, Vector2i>? Resized;

    HostScreenRef Ref { get; }
    Vector2i ClientSize { get; set; }
    string Title { get; set; }
    TextureFormat SurfaceFormat { get; }
    GraphicsBackend Backend { get; }
    Texture DepthTexture { get; }
    TextureView DepthTextureView { get; }
    SurfaceTextureView SurfaceTextureView { get; }
}

internal static class HostScreenExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rust.Ref<CE.HostScreen> AsRefChecked(this IHostScreen screen)
    {
        return screen.Ref.AsRefChecked();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rust.Ref<CE.HostScreen> AsRefUnchecked(this IHostScreen screen)
    {
        return screen.Ref.AsRefUnchecked();
    }
}
