#nullable enable
using Elffy.NativeBind;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Elffy;

public static class Engine
{
    private static readonly List<HostScreen> _screens = new List<HostScreen>();
    private static Action<IHostScreen>? _onInitialized;

    public static void Start(in HostScreenConfig screenConfig, Action<IHostScreen> onInitialized)
    {
        ArgumentNullException.ThrowIfNull(onInitialized);
        _onInitialized = onInitialized;
        var engineConfig = new EngineCoreConfig
        {
            OnStart = _onStart,
            OnRedrawRequested = _onRedrawRequested,
            OnCleared = _onCleared,
            OnResized = _onResized,
        };
        EngineCore.EngineStart(engineConfig, screenConfig);
    }

    private static readonly Action<Box<CE.HostScreen>, CE.HostScreenInfo, CE.HostScreenId> _onStart =
        (Box<CE.HostScreen> screenHandle, CE.HostScreenInfo info, CE.HostScreenId id) =>
        {
            var screen = new HostScreen(screenHandle, id);
            _screens.Add(screen);
            screen.OnInitialize(info);
            _onInitialized?.Invoke(screen);
        };

    private static readonly Action<CE.HostScreenId> _onRedrawRequested =
        (CE.HostScreenId id) =>
        {
            if(TryFindScreen(id, out var screen) == false) {
                Debug.Fail("HostScreen should be found");
                return;
            }
            screen.OnRedrawRequested();
        };

    private static readonly Action<CE.HostScreenId> _onCleared =
        (CE.HostScreenId id) =>
        {
            if(TryFindScreen(id, out var screen) == false) {
                Debug.Fail("HostScreen should be found");
                return;
            }
            screen.OnCleared();
        };

    private static readonly Action<CE.HostScreenId, uint, uint> _onResized =
        (CE.HostScreenId id, uint width, uint height) =>
        {
            if(TryFindScreen(id, out var screen) == false) {
                Debug.Fail("HostScreen should be found");
                return;
            }
            screen.OnResized(width, height);
        };

    private static bool TryFindScreen(CE.HostScreenId id, [MaybeNullWhen(false)] out HostScreen screen)
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
    public required Action<Box<CE.HostScreen>, CE.HostScreenInfo, CE.HostScreenId> OnStart { get; init; }
    public required Action<CE.HostScreenId> OnRedrawRequested { get; init; }
    public required Action<CE.HostScreenId> OnCleared { get; init; }

    public required Action<CE.HostScreenId, u32, u32> OnResized { get; init; }
}
