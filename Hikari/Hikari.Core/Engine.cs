#nullable enable
using Cysharp.Threading.Tasks;
using Hikari.NativeBind;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hikari;

public static class Engine
{
    private static readonly Dictionary<CH.ScreenId, Screen> _screens = new();

    private static Action<Screen>? _onScreenInit;

    public static void Run(in ScreenConfig screenConfig, Func<Screen, UniTask> onScreenInit)
    {
        Run(in screenConfig, screen =>
        {
            UniTask.Void(
                async static arg =>
                {
                    var (onScreenInit, screen) = arg;
                    try {
                        await onScreenInit(screen);
                    }
                    catch(Exception ex) {
                        Console.Error.WriteLine(ex);
                    }
                },
                (onScreenInit, screen));
        });
    }

    public static void Run(in ScreenConfig screenConfig, Func<Screen, ValueTask> onScreenInit)
    {
        Run(in screenConfig, screen =>
        {
            UniTask.Void(
                async static arg =>
                {
                    var (onScreenInit, screen) = arg;
                    try {
                        await onScreenInit(screen);
                    }
                    catch(Exception ex) {
                        Console.Error.WriteLine(ex);
                    }
                },
                (onScreenInit, screen));
        });
    }

    public static void Run(in ScreenConfig screenConfig, Func<Screen, Task> onScreenInit)
    {
        Run(in screenConfig, screen =>
        {
            UniTask.Void(
                async static arg =>
                {
                    var (onScreenInit, screen) = arg;
                    try {
                        await onScreenInit(screen);
                    }
                    catch(Exception ex) {
                        Console.Error.WriteLine(ex);
                    }
                },
                (onScreenInit, screen));
        });
    }

    public static void Run(in ScreenConfig screenConfig, Action<Screen> onScreenInit)
    {
        ArgumentNullException.ThrowIfNull(onScreenInit);
        CheckPlatformBackend(screenConfig.Backend);
        if(Interlocked.CompareExchange(ref _onScreenInit, onScreenInit, null) != null) {
            throw new InvalidOperationException("The engine is already running.");
        }
        if(screenConfig.UseSynchronizationContext) {
            HikariSynchronizationContext.Install(out _);
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

    private static void CheckPlatformBackend(GraphicsBackend backend)
    {
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            var ok = backend is GraphicsBackend.Dx12 or GraphicsBackend.Vulkan;
            if(ok == false) {
                throw new PlatformNotSupportedException($"'{nameof(GraphicsBackend.Dx12)}' or '{nameof(GraphicsBackend.Vulkan)}' is only supported backend in the current platform");
            }
        }
        else if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            var ok = backend is GraphicsBackend.Metal;
            if(ok == false) {
                throw new PlatformNotSupportedException($"'{nameof(GraphicsBackend.Metal)}' is only supported backend in the current platform");
            }
        }
        else {
            throw new PlatformNotSupportedException($"The current platform is not supported.");
        }
    }

    private static readonly Func<Rust.Box<CH.Screen>, CH.ScreenInfo, CH.ScreenId> _onStart =
        (Rust.Box<CH.Screen> screenHandle, CH.ScreenInfo info) =>
        {
            var mainThread = ThreadId.CurrentThread();
            var syncContextReceiver = (AsyncOperationManager.SynchronizationContext as HikariSynchronizationContext)?.Receiver;
            var screen = new Screen(screenHandle, mainThread, _onScreenInit, syncContextReceiver);
            var id = screen.ScreenId;
            _screens.Add(id, screen);
            screen.OnInitialize(info);
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

    private static readonly Action<CH.ScreenId, CH.KeyCode, bool> _onKeyboardInput =
        (CH.ScreenId id, CH.KeyCode key, bool pressed) =>
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

    private static readonly Func<CH.ScreenId, Rust.OptionBox<CH.Screen>> _onClosed =
        (CH.ScreenId id) =>
        {
            if(_screens.Remove(id, out var screen) == false) {
                return Rust.OptionBox<CH.Screen>.None;
            }
            var screenRaw = screen.OnClosed();
            return screenRaw;
        };
}
