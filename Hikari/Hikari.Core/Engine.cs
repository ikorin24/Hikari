#nullable enable
using Hikari.NativeBind;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Hikari;

public static class Engine
{
    private static readonly Dictionary<CE.ScreenId, Screen> _screens = new();

    private static Action<Screen>? _onInitialized;

    public static void Run(in ScreenConfig screenConfig, Action<Screen> onInitialized)
    {
        ArgumentNullException.ThrowIfNull(onInitialized);
        if(Interlocked.CompareExchange(ref _onInitialized, onInitialized, null) != null) {
            throw new InvalidOperationException("The engine is already running.");
        }
        var engineConfig = new EngineCoreConfig
        {
            OnStart = _onStart,
            OnRedrawRequested = _onRedrawRequested,
            OnCleared = _onCleared,
            OnResized = _onResized,
            OnKeyboardInput = _onKeyboardInput,
            OnCharReceived = _onCharReceived,
            OnMouseButton = _onMouseButon,
            OnImeInput = _onImeInput,
            OnWheel = _onWheel,
            OnCursorMoved = _onCursorMoved,
            OnCursorEnteredLeft = _onCursorEnteredLeft,
            OnClosing = _onClosing,
            OnClosed = _onClosed,
        };
        EngineCore.EngineStart(engineConfig, screenConfig);
    }

    private static readonly Func<Rust.Box<CE.HostScreen>, CE.HostScreenInfo, CE.ScreenId> _onStart =
        (Rust.Box<CE.HostScreen> screenHandle, CE.HostScreenInfo info) =>
        {
            var mainThread = ThreadId.CurrentThread();
            var screen = new Screen(screenHandle, mainThread);
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
            // TODO: width == 0 && height == 0 when window is minimized
            Debug.Assert(width != 0);
            Debug.Assert(height != 0);
            _screens[id].OnResized(width, height);
        };

    private static readonly Action<CE.ScreenId, Winit.VirtualKeyCode, bool> _onKeyboardInput =
        (CE.ScreenId id, Winit.VirtualKeyCode key, bool pressed) =>
        {
            _screens[id].Keyboard.OnKeyboardInput(key, pressed);
        };
    private static readonly Action<CE.ScreenId, Rune> _onCharReceived =
        (CE.ScreenId id, Rune input) =>
        {
            _screens[id].Keyboard.OnCharReceived(input);
        };

    private static readonly Action<CE.ScreenId, CE.MouseButton, bool> _onMouseButon =
        (CE.ScreenId id, CE.MouseButton button, bool pressed) =>
        {
            _screens[id].Mouse.OnMouseButton(button, pressed);
        };

    private static readonly EngineCoreImeInputAction _onImeInput =
        (CE.ScreenId id, in CE.ImeInputData input) =>
        {
            _screens[id].Keyboard.OnImeInput(input);
        };

    private static readonly Action<CE.ScreenId, f32, f32> _onWheel =
        (CE.ScreenId id, f32 xDelta, f32 yDelta) =>
        {
            _screens[id].Mouse.OnWheel(new Vector2(xDelta, yDelta));
        };

    private static readonly Action<CE.ScreenId, f32, f32> _onCursorMoved =
        (CE.ScreenId id, f32 x, f32 y) =>
        {
            _screens[id].Mouse.OnCursorMoved(new Vector2(x, y));
        };

    private static readonly Action<CE.ScreenId, bool> _onCursorEnteredLeft =
        (CE.ScreenId id, bool entered) =>
        {
            _screens[id].Mouse.OnCursorEnteredLeft(entered);
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
