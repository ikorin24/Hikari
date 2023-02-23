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

    public static void Run(in HostScreenConfig screenConfig, Action<IHostScreen> onInitialized)
    {
        ArgumentNullException.ThrowIfNull(onInitialized);
        _onInitialized = onInitialized;
        var engineConfig = new EngineCoreConfig
        {
            OnStart = _onStart,
            OnFindScreen = _idToScreenNative,
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
            _screens.Add(screenOwn);
            screen.OnInitialize(info);
            _onInitialized?.Invoke(screen);
            return screen.ScreenId;
        };

    private static readonly FindScreenFunc _idToScreenNative = (CE.ScreenId id) => id.ToScreen();

    private static readonly ScreenAction _onRedrawRequested =
        (Rust.Ref<CE.HostScreen> screenNative) =>
        {
            if(TryFindScreen(screenNative, out var screen) == false) {
                Debug.Fail("HostScreen should be found");
                return;
            }
            screen.OnRedrawRequested();
        };

    private static readonly ScreenAction _onCleared =
        (Rust.Ref<CE.HostScreen> screenNative) =>
        {
            if(TryFindScreen(screenNative, out var screen) == false) {
                Debug.Fail("HostScreen should be found");
                return;
            }
            screen.OnCleared();
        };

    private static readonly ScreenAction<uint, uint> _onResized =
        (Rust.Ref<CE.HostScreen> screenNative, uint width, uint height) =>
        {
            if(TryFindScreen(screenNative, out var screen) == false) {
                Debug.Fail("HostScreen should be found");
                return;
            }
            screen.OnResized(width, height);
        };

    private static readonly ScreenAction<Winit.VirtualKeyCode, bool> _onKeyboardInput =
        (Rust.Ref<CE.HostScreen> screenNative, Winit.VirtualKeyCode key, bool pressed) =>
        {
            if(TryFindScreen(screenNative, out var screen) == false) {
                Debug.Fail("HostScreen should be found");
                return;
            }
            screen.OnKeyboardInput(key, pressed);
        };
    private static readonly ScreenAction<Rune> _onCharReceived =
        (Rust.Ref<CE.HostScreen> screenNative, Rune input) =>
        {
            if(TryFindScreen(screenNative, out var screen) == false) {
                Debug.Fail("HostScreen should be found");
                return;
            }
            screen.OnCharReceived(input);
        };

    private static readonly EngineCoreScreenClosingAction _onClosing =
        (Rust.Ref<CE.HostScreen> screenNative, ref bool cancel) =>
        {
            if(TryFindScreen(screenNative, out var screen) == false) {
                Debug.Fail("HostScreen should be found");
                return;
            }
            screen.OnClosing(ref cancel);
        };

    private static bool TryFindScreen(Rust.Ref<CE.HostScreen> native, [MaybeNullWhen(false)] out HostScreen screen)
    {
        var screens = (ReadOnlySpan<Own<HostScreen>>)CollectionsMarshal.AsSpan(_screens);
        foreach(var screenOwn in screens) {
            var s = screenOwn.AsValue();
            if(s.AsRefUnchecked().AsPtr() == native.AsPtr()) {
                screen = s;
                return true;
            }
        }
        screen = null;
        return false;
    }
}
