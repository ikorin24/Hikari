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
    private static readonly Dictionary<CE.ScreenId, HostScreen> _screens = new();

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
            OnImeInput = _onImeInput,
            OnClosing = _onClosing,
            OnClosed = _onClosed,
        };
        EngineCore.EngineStart(engineConfig, screenConfig);
    }

    private static readonly Func<Rust.Box<CE.HostScreen>, CE.HostScreenInfo, CE.ScreenId> _onStart =
        (Rust.Box<CE.HostScreen> screenHandle, CE.HostScreenInfo info) =>
        {
            var screen = HostScreen.Create(screenHandle);
            var id = screen.ScreenId;
            _screens.Add(id, screen);
            screen.OnInitialize(info);
            _onInitialized?.Invoke(screen);
            return id;
        };

    private static readonly Func<CE.ScreenId, bool> _onRedrawRequested =
        (CE.ScreenId id) =>
        {
            return _screens[id].OnRedrawRequested();
        };

    private static readonly Action<CE.ScreenId> _onCleared =
        (CE.ScreenId id) =>
        {
            _screens[id].OnCleared();
        };

    private static readonly Action<CE.ScreenId, uint, uint> _onResized =
        (CE.ScreenId id, uint width, uint height) =>
        {
            _screens[id].OnResized(width, height);
        };

    private static readonly Action<CE.ScreenId, Winit.VirtualKeyCode, bool> _onKeyboardInput =
        (CE.ScreenId id, Winit.VirtualKeyCode key, bool pressed) =>
        {
            _screens[id].OnKeyboardInput(key, pressed);
        };
    private static readonly Action<CE.ScreenId, Rune> _onCharReceived =
        (CE.ScreenId id, Rune input) =>
        {
            _screens[id].OnCharReceived(input);
        };

    private static readonly ImeState _imeState = new ImeState();   // TODO: not static
    private static readonly EngineCoreImeInputAction _onImeInput =
        (CE.ScreenId id, in CE.ImeInputData input) =>
        {
            //_screens[id].OnCharReceived(input);
            _imeState.OnInput(input);
        };

    private static readonly EngineCoreScreenClosingAction _onClosing =
        (CE.ScreenId id, ref bool cancel) =>
        {
            _screens[id].OnClosing(ref cancel);
        };

    private static readonly Func<CE.ScreenId, Rust.OptionBox<CE.HostScreen>> _onClosed =
        (CE.ScreenId id) =>
        {
            if(_screens.Remove(id, out var screen) == false) {
                return Rust.OptionBox<CE.HostScreen>.None;
            }
            return screen.OnClosed();
        };
}
