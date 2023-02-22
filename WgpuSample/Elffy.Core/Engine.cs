#nullable enable
using Elffy.NativeBind;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace Elffy;

public static class Engine
{
    private static readonly List<Own<HostScreen>> _screens = new List<Own<HostScreen>>();
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
            OnKeyboardInput = _onKeyboardInput,
            OnCharReceived = _onCharReceived,
        };
        EngineCore.EngineStart(engineConfig, screenConfig);
    }

    private static readonly Action<Rust.Box<CE.HostScreen>, CE.HostScreenInfo, CE.HostScreenId> _onStart =
        (Rust.Box<CE.HostScreen> screenHandle, CE.HostScreenInfo info, CE.HostScreenId id) =>
        {
            var screenOwn = HostScreen.Create(screenHandle, id);
            var screen = screenOwn.AsValue();
            _screens.Add(screenOwn);
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

    private static readonly Action<CE.HostScreenId, Winit.VirtualKeyCode, bool> _onKeyboardInput =
        (CE.HostScreenId id, Winit.VirtualKeyCode key, bool pressed) =>
        {
            if(TryFindScreen(id, out var screen) == false) {
                Debug.Fail("HostScreen should be found");
                return;
            }
            screen.OnKeyboardInput(key, pressed);
        };
    private static readonly Action<CE.HostScreenId, Rune> _onCharReceived =
        (CE.HostScreenId id, Rune input) =>
        {
            if(TryFindScreen(id, out var screen) == false) {
                Debug.Fail("HostScreen should be found");
                return;
            }
            screen.OnCharReceived(input);
        };

    private static bool TryFindScreen(CE.HostScreenId id, [MaybeNullWhen(false)] out HostScreen screen)
    {
        nuint idNum = id.AsNumber();
        var screens = (ReadOnlySpan<Own<HostScreen>>)CollectionsMarshal.AsSpan(_screens);
        foreach(var screenOwn in screens) {
            var s = screenOwn.AsValue();
            if(s.Id == idNum) {
                screen = s;
                return true;
            }
        }
        screen = null;
        return false;
    }
}
