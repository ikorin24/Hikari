#nullable enable
using Elffy.Bind;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using EC = Elffy.Bind.Elffycore;

namespace Elffy;

internal static class Engine
{
    private static readonly List<HostScreen> _screens = new List<HostScreen>();

    public static event Action<IHostScreen> Init;

    public static void Start()
    {
        var engineConfig = new EngineCoreConfig
        {
            OnStart = _onStart,
            OnRedrawRequested = _onRedrawRequested,
            OnCleared = _onCleared,
            OnResized = _onResized,
        };
        var screenConfig = new HostScreenConfig
        {
            Backend = Wgpu.Backends.DX12,
            Width = 1280,
            Height = 720,
            Style = WindowStyle.Default,
        };
        EngineCore.EngineStart(engineConfig, screenConfig);
    }

    private static readonly Action<Box<EC.HostScreen>, EC.HostScreenInfo, EC.HostScreenId> _onStart =
        (Box<EC.HostScreen> screenHandle, EC.HostScreenInfo info, EC.HostScreenId id) =>
        {
            var screen = new HostScreen(screenHandle, id);
            _screens.Add(screen);
            screen.OnInitialize();
            Init?.Invoke(screen);
        };

    private static readonly Action<EC.HostScreenId> _onRedrawRequested =
        (EC.HostScreenId id) =>
        {
            if(TryFindScreen(id, out var screen) == false) {
                Debug.Fail("HostScreen should be found");
                return;
            }
        };

    private static readonly Action<EC.HostScreenId> _onCleared =
        (EC.HostScreenId id) =>
        {
            if(TryFindScreen(id, out var screen) == false) {
                Debug.Fail("HostScreen should be found");
                return;
            }
            screen.OnCleared();
        };

    private static readonly Action<EC.HostScreenId, uint, uint> _onResized =
        (EC.HostScreenId id, uint width, uint height) =>
        {
            if(TryFindScreen(id, out var screen) == false) {
                Debug.Fail("HostScreen should be found");
                return;
            }
            screen.OnResized(width, height);
        };

    private static bool TryFindScreen(EC.HostScreenId id, [MaybeNullWhen(false)] out HostScreen screen)
    {
        nuint idNum = id.AsNumber();
        var screens = (ReadOnlySpan<HostScreen>)CollectionsMarshal.AsSpan(_screens);
        foreach(var s in screens) {
            if(s.Id == idNum) {
                screen = s;
                return true;
            }
        }
        screen = null;
        return false;
    }
}

internal readonly struct EngineCoreConfig
{
    public required Action<Box<EC.HostScreen>, EC.HostScreenInfo, EC.HostScreenId> OnStart { get; init; }
    public required Action<EC.HostScreenId> OnRedrawRequested { get; init; }
    public required Action<EC.HostScreenId> OnCleared { get; init; }

    public required Action<EC.HostScreenId, u32, u32> OnResized { get; init; }
}

internal readonly ref struct HostScreenConfig
{
    public required WindowStyle Style { get; init; }
    public required u32 Width { get; init; }
    public required u32 Height { get; init; }
    public required Wgpu.Backends Backend { get; init; }
}
