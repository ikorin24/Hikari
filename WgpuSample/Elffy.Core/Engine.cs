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
    //private static readonly List<Own<HostScreen>> _screens = new List<Own<HostScreen>>();

    private static readonly Dictionary<CE.ScreenId, Own<HostScreen>> _screens = new();

    private static Action<IHostScreen>? _onInitialized;

    public static void Run(in HostScreenConfig screenConfig, Action<IHostScreen> onInitialized)
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
            OnClosing = _onClosing,
        };
        EngineCore.EngineStart(engineConfig, screenConfig);
    }

    private static readonly Func<Rust.Box<CE.HostScreen>, CE.HostScreenInfo, CE.ScreenId> _onStart =
        (Rust.Box<CE.HostScreen> screenHandle, CE.HostScreenInfo info) =>
        {
            var screenOwn = HostScreen.Create(screenHandle);
            var screen = screenOwn.AsValue();
            var id = screen.ScreenId;
            _screens.Add(id, screenOwn);
            screen.OnInitialize(info);
            _onInitialized?.Invoke(screen);
            return id;
        };

    private static readonly Func<CE.ScreenId, bool> _onRedrawRequested =
        (CE.ScreenId id) =>
        {
            if(TryGetScreen(id, out var screen) == false) {
                Debug.Fail("HostScreen should be found");
                return false;
            }
            return screen.OnRedrawRequested();
        };

    private static readonly Action<CE.ScreenId> _onCleared =
        (CE.ScreenId id) =>
        {
            var screen = GetScreen(id);
            screen.OnCleared();
        };

    private static readonly Action<CE.ScreenId, uint, uint> _onResized =
        (CE.ScreenId id, uint width, uint height) =>
        {
            var screen = GetScreen(id);
            screen.OnResized(width, height);
        };

    private static readonly Action<CE.ScreenId, Winit.VirtualKeyCode, bool> _onKeyboardInput =
        (CE.ScreenId id, Winit.VirtualKeyCode key, bool pressed) =>
        {
            var screen = GetScreen(id);
            screen.OnKeyboardInput(key, pressed);
        };
    private static readonly Action<CE.ScreenId, Rune> _onCharReceived =
        (CE.ScreenId id, Rune input) =>
        {
            var screen = GetScreen(id);
            screen.OnCharReceived(input);
        };

    private static readonly EngineCoreScreenClosingAction _onClosing =
        (CE.ScreenId id, ref bool cancel) =>
        {
            var screen = GetScreen(id);
            screen.OnClosing(ref cancel);
        };

    private static HostScreen GetScreen(CE.ScreenId id)
    {
        if(TryGetScreen(id, out var screen) == false) {
            Throw(id);

            [DoesNotReturn] static void Throw(CE.ScreenId id) => throw new InvalidOperationException($"No HostScreen (id={id})");
        }
        return screen;
    }

    private static bool TryGetScreen(CE.ScreenId id, [MaybeNullWhen(false)] out HostScreen screen)
    {
        if(_screens.TryGetValue(id, out var screenOwn) == false) {
            screen = null;
            return false;
        }
        screen = screenOwn.AsValue();
        return true;
    }
}
