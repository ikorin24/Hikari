#nullable enable
using Cysharp.Threading.Tasks;
using Hikari.NativeBind;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Threading;

namespace Hikari;

public sealed class Engine
{
    private readonly Dictionary<CH.ScreenId, Screen> _screens = new();
    private static readonly Dictionary<CH.EngineId, EngineInstance> _instances = new();
    private sealed record EngineInstance
    {
        public required EngineCoreConfig Config { get; init; }
        public required Rust.Box<CH.EngineProxy> EngineProxy { get; init; }
    }


    private Engine()
    {
    }

    public static void Run(in ScreenConfig screenConfig, Func<Screen, UniTask> onStart)
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
                (onStart, screen));
        });
    }

    public static void Run(in ScreenConfig screenConfig, Func<Screen, ValueTask> onStart)
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
                (onStart, screen));
        });
    }

    public static void Run(in ScreenConfig screenConfig, Func<Screen, Task> onStart)
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
                (onStart, screen));
        });
    }

    public static unsafe void Run(in ScreenConfig screenConfig, Action<Screen> onStart)
    {
        ArgumentNullException.ThrowIfNull(onStart);
        CheckPlatformBackend(screenConfig.Backend);
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            if(Thread.CurrentThread.GetApartmentState() != ApartmentState.STA) {
                throw new InvalidOperationException("The thread should be STA. (for C#, mark main method as [STAThread] attribute.)");
            }
        }
        var title = screenConfig.Title;

        if(screenConfig.UseSynchronizationContext) {
            HikariSynchronizationContext.Install(out _);
        }
        var engine = new Engine();
        var config = new EngineCoreConfig
        {
            OnScreenInit = (screenHandle, info) =>
            {
                var mainThread = ThreadId.CurrentThread();
                var syncContextReceiver = (AsyncOperationManager.SynchronizationContext as HikariSynchronizationContext)?.Receiver;
                var screenId = new CH.ScreenId(screenHandle);
                var screen = new Screen(screenHandle, mainThread, onStart, syncContextReceiver, info);
                screen.Title = title;
                engine._screens.Add(screenId, screen);
                return screenId;
            },
            OnRedrawRequested = engine.OnRedrawRequested,
            OnCleared = engine.OnCleared,
            OnResized = engine.OnResized,
            OnKeyboardInput = engine.OnKeyboardInput,
            OnCharReceived = engine.OnCharReceived,
            OnMouseButton = engine.OnMouseButon,
            OnImeInput = engine.OnImeInput,
            OnWheel = engine.OnWheel,
            OnCursorMoved = engine.OnCursorMoved,
            OnCursorEnteredLeft = engine.OnCursorEnteredLeft,
            OnClosing = engine.OnClosing,
            OnClosed = engine.OnClosed,
        };

        var engineConfigNative = new CH.EngineCoreConfig
        {
            on_engine_init = new(&OnEngineInit),
            on_engine_closed = new(&OnEngineClosed),
            on_screen_init = new(&OnScreenInit),
            on_unhandled_error = new(&OnUnhandledError),
            event_cleared = new(&EventCleared),
            event_redraw_requested = new(&EventRedrawRequested),
            event_resized = new(&EventResized),
            event_keyboard = new(&EventKeyboard),
            event_char_received = new(&EventCharReceived),
            event_mouse_button = new(&EventMouseButton),
            event_ime = new(&EventIme),
            event_wheel = new(&EventWheel),
            event_cursor_moved = new(&EventCursorMoved),
            event_cursor_entered_left = new(&EventCursorEnteredLeft),
            event_closing = new(&EventClosing),
            event_closed = new(&EventClosed),
            debug_println = new(&DebugPrintln),
        };

        var screenConfigNative = screenConfig.ToCoreType();
#pragma warning disable CS8500 // get address to managed type
        void* state = &config;
#pragma warning restore CS8500 // get address to managed type
        EngineCore.EngineStart(state, &engineConfigNative, &screenConfigNative);
        return;

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        unsafe static CH.EngineId OnEngineInit(void* state, Rust.Box<CH.EngineProxy> proxy)
        {
#pragma warning disable CS8500 // get address to managed type
            var config = *(EngineCoreConfig*)state;
#pragma warning restore CS8500 // get address to managed type

            var engineId = new CH.EngineId(proxy.AsPtr());
            var instance = new EngineInstance
            {
                Config = config,
                EngineProxy = proxy,
            };
            _instances.Add(engineId, instance);
            return engineId;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        unsafe static Rust.OptionBox<CH.EngineProxy> OnEngineClosed(CH.EngineId engineId)
        {
            if(_instances.Remove(engineId, out var instance)) {
                return instance.EngineProxy;
            }
            else {
                return Rust.OptionBox<CH.EngineProxy>.None;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static CH.ScreenId OnScreenInit(
            CH.EngineId engineId,
            Rust.Box<CH.Screen> screen,
            CH.ScreenInfo* info
            )
        {
            return _instances[engineId].Config.OnScreenInit(screen, *info);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void OnUnhandledError(byte* message, nuint len)
        {
            try {
                var str = Encoding.UTF8.GetString(message, (int)len);
                Console.Error.WriteLine(str);
#if DEBUG
                System.Diagnostics.Debug.WriteLine(str);
                System.Diagnostics.Debugger.Break();
                Environment.Exit(-1);
#endif
            }
            catch {
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void EventCleared(CH.EngineId engineId, CH.ScreenId id)
        {
            _instances[engineId].Config.OnCleared(id);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static bool EventRedrawRequested(CH.EngineId engineId, CH.ScreenId id)
        {
            return _instances[engineId].Config.OnRedrawRequested(id);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void EventResized(CH.EngineId engineId, CH.ScreenId id, u32 width, u32 height)
        {
            _instances[engineId].Config.OnResized(id, width, height);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void EventKeyboard(CH.EngineId engineId, CH.ScreenId id, CH.KeyCode key, bool pressed)
        {
            _instances[engineId].Config.OnKeyboardInput(id, key, pressed);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void EventCharReceived(CH.EngineId engineId, CH.ScreenId id, Rune input)
        {
            _instances[engineId].Config.OnCharReceived(id, input);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void EventMouseButton(CH.EngineId engineId, CH.ScreenId id, CH.MouseButton button, bool pressed)
        {
            _instances[engineId].Config.OnMouseButton(id, button, pressed);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void EventIme(CH.EngineId engineId, CH.ScreenId id, CH.ImeInputData* input)
        {
            _instances[engineId].Config.OnImeInput(id, in *input);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void EventWheel(CH.EngineId engineId, CH.ScreenId id, f32 x_delta, f32 y_delta)
        {
            _instances[engineId].Config.OnWheel(id, x_delta, y_delta);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void EventCursorMoved(CH.EngineId engineId, CH.ScreenId id, f32 x, f32 y)
        {
            _instances[engineId].Config.OnCursorMoved(id, x, y);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void EventCursorEnteredLeft(CH.EngineId engineId, CH.ScreenId id, bool entered)
        {
            _instances[engineId].Config.OnCursorEnteredLeft(id, entered);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void EventClosing(CH.EngineId engineId, CH.ScreenId id, bool* mut_cancel)
        {
            ref bool cancel = ref *mut_cancel;
            _instances[engineId].Config.OnClosing(id, ref cancel);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static Rust.OptionBox<CH.Screen> EventClosed(CH.EngineId engineId, CH.ScreenId id)
        {
            return _instances[engineId].Config.OnClosed(id);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void DebugPrintln(CH.EngineId engineId, u8* message, usize len)
        {
            var length = (int)usize.Min(len, int.MaxValue);
            var str = Encoding.UTF8.GetString(message, length);
            Debug.WriteLine(str);
        }
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

    private bool OnRedrawRequested(CH.ScreenId id) => _screens[id].OnRedrawRequested();

    private void OnCleared(CH.ScreenId id) => _screens[id].OnCleared();

    private void OnResized(CH.ScreenId id, uint width, uint height)
    {
        if(width == 0 || height == 0) {
            return;
        }
        _screens[id].OnResized(width, height);
    }

    private void OnKeyboardInput(CH.ScreenId id, CH.KeyCode key, bool pressed) => _screens[id].Keyboard.OnKeyboardInput(key, pressed);

    private void OnCharReceived(CH.ScreenId id, Rune input) => _screens[id].Keyboard.OnCharReceived(input);

    private void OnMouseButon(CH.ScreenId id, CH.MouseButton button, bool pressed) => _screens[id].Mouse.OnMouseButton(button, pressed);

    private void OnImeInput(CH.ScreenId id, in CH.ImeInputData input) => _screens[id].Keyboard.OnImeInput(input);

    private void OnWheel(CH.ScreenId id, f32 xDelta, f32 yDelta) => _screens[id].Mouse.OnWheel(new Vector2(xDelta, yDelta));

    private void OnCursorMoved(CH.ScreenId id, f32 x, f32 y) => _screens[id].Mouse.OnCursorMoved(new Vector2(x, y));

    private void OnCursorEnteredLeft(CH.ScreenId id, bool entered) => _screens[id].Mouse.OnCursorEnteredLeft(entered);

    private void OnClosing(CH.ScreenId id, ref bool cancel) => _screens[id].OnClosing(ref cancel);

    private Rust.OptionBox<CH.Screen> OnClosed(CH.ScreenId id)
    {
        if(_screens.Remove(id, out var screen) == false) {
            return Rust.OptionBox<CH.Screen>.None;
        }
        var screenRaw = screen.OnClosed();
        return screenRaw;
    }
}
