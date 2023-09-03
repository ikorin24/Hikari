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
    private static readonly Dictionary<CH.ScreenId, Screen> _screens = new();

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

    private static readonly Func<Rust.Box<CH.HostScreen>, CH.HostScreenInfo, CH.ScreenId> _onStart =
        (Rust.Box<CH.HostScreen> screenHandle, CH.HostScreenInfo info) =>
        {
            var mainThread = ThreadId.CurrentThread();
            var screen = new Screen(screenHandle, mainThread);
            var id = screen.ScreenId;
            _screens.Add(id, screen);
            screen.OnInitialize(info);
            _onInitialized?.Invoke(screen);
            return id;
        };

    private static readonly Func<CH.ScreenId, bool> _onRedrawRequested =
        (CH.ScreenId id) =>
        {
            return _screens[id].OnRedrawRequested();
        };

    private static readonly Action<CH.ScreenId> _onCleared =
        (CH.ScreenId id) =>
        {
            _screens[id].OnCleared();
        };

    private static readonly Action<CH.ScreenId, uint, uint> _onResized =
        (CH.ScreenId id, uint width, uint height) =>
        {
            if(width == 0 || height == 0) {
                return;
            }
            _screens[id].OnResized(width, height);
        };

    private static readonly Action<CH.ScreenId, Winit.VirtualKeyCode, bool> _onKeyboardInput =
        (CH.ScreenId id, Winit.VirtualKeyCode key, bool pressed) =>
        {
            _screens[id].Keyboard.OnKeyboardInput(key, pressed);
        };
    private static readonly Action<CH.ScreenId, Rune> _onCharReceived =
        (CH.ScreenId id, Rune input) =>
        {
            _screens[id].Keyboard.OnCharReceived(input);
        };

    private static readonly Action<CH.ScreenId, CH.MouseButton, bool> _onMouseButon =
        (CH.ScreenId id, CH.MouseButton button, bool pressed) =>
        {
            _screens[id].Mouse.OnMouseButton(button, pressed);
        };

    private static readonly EngineCoreImeInputAction _onImeInput =
        (CH.ScreenId id, in CH.ImeInputData input) =>
        {
            _screens[id].Keyboard.OnImeInput(input);
        };

    private static readonly Action<CH.ScreenId, f32, f32> _onWheel =
        (CH.ScreenId id, f32 xDelta, f32 yDelta) =>
        {
            _screens[id].Mouse.OnWheel(new Vector2(xDelta, yDelta));
        };

    private static readonly Action<CH.ScreenId, f32, f32> _onCursorMoved =
        (CH.ScreenId id, f32 x, f32 y) =>
        {
            _screens[id].Mouse.OnCursorMoved(new Vector2(x, y));
        };

    private static readonly Action<CH.ScreenId, bool> _onCursorEnteredLeft =
        (CH.ScreenId id, bool entered) =>
        {
            _screens[id].Mouse.OnCursorEnteredLeft(entered);
        };

    private static readonly EngineCoreScreenClosingAction _onClosing =
        (CH.ScreenId id, ref bool cancel) =>
        {
            _screens[id].OnClosing(ref cancel);
        };

    private static readonly Func<CH.ScreenId, Rust.OptionBox<CH.HostScreen>> _onClosed =
        (CH.ScreenId id) =>
        {
            if(_screens.Remove(id, out var screen) == false) {
                return Rust.OptionBox<CH.HostScreen>.None;
            }
            return screen.OnClosed();
        };
}
