#nullable enable
using Cysharp.Threading.Tasks;
using Hikari.NativeBind;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Concurrent;

namespace Hikari;

public sealed class Engine
{
    private static readonly Dictionary<CH.EngineId, Engine> _engines = new();

    private readonly Dictionary<CH.ScreenId, Screen> _screens = new();
    private Rust.OptionBox<CH.EngineProxy> _proxy;
    private readonly Lock _proxyLock = new Lock();
    private readonly ConcurrentDictionary<ulong, Action<Screen>> _callbacks = new();
    private ulong _callbackIdGen = 0;

    private Engine(Rust.Box<CH.EngineProxy> proxy)
    {
        _proxy = proxy;
    }

    public void CreateScreen(in ScreenConfig config, Func<Screen, UniTask> onStart)
    {
        CreateScreen(
            config,
            screen => UniTask.Void(async static arg =>
            {
                var (screen, onStart) = arg;
                try {
                    await onStart(screen);
                }
                catch(Exception ex) {
                    Console.Error.WriteLine(ex);
                }
            }, (screen, onStart)));
    }

    public void CreateScreen(in ScreenConfig config, Action<Screen> onStart)
    {
        ArgumentNullException.ThrowIfNull(onStart);
        if(config.Backend != null) {
            CheckPlatformBackend(config.Backend.Value);
        }
        var callbackId = Interlocked.Increment(ref _callbackIdGen);
        _callbacks[callbackId] = onStart;
        try {
            lock(_proxyLock) {
                if(_proxy.IsSome(out var proxy)) {
                    proxy.AsRef().CreateScreen(callbackId, config);
                }
                else {
                    ThrowAlreadyStopped();
                }
            }
        }
        catch {
            _callbacks.TryRemove(callbackId, out _);
            throw;
        }
    }

    [DoesNotReturn]
    private static void ThrowAlreadyStopped()
    {
        throw new InvalidOperationException("Engine is already stopped");
    }

    public static unsafe void Run(Action<Engine> onStart)
    {
        ArgumentNullException.ThrowIfNull(onStart);
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            var apartmentState = Thread.CurrentThread.GetApartmentState();
            if(apartmentState != ApartmentState.STA) {
                throw new InvalidOperationException($"Current thread aprtment is {apartmentState}. It should be STA. (for C#, mark main method as [STAThread] attribute.)");
            }
        }
        HikariSynchronizationContext.InstallIfNeeded(out _, out _);

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

        EngineCore.EngineStart(onStart, &engineConfigNative);
        return;

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
#pragma warning disable CS8500 // Use address to managed type
        unsafe static CH.EngineId OnEngineInit(Action<Engine>* state, Rust.Box<CH.EngineProxy> proxy)
#pragma warning restore CS8500 // Use address to managed type
        {
            var engine = new Engine(proxy);
            var engineId = new CH.EngineId(proxy.AsPtr());
            _engines.Add(engineId, engine);
            var onStart = *state;
            onStart.Invoke(engine);
            return engineId;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        unsafe static Rust.OptionBox<CH.EngineProxy> OnEngineClosed(CH.EngineId engineId)
        {
            if(_engines.Remove(engineId, out var engine)) {
                lock(engine._proxyLock) {
                    var proxy = engine._proxy;
                    engine._proxy = Rust.OptionBox<CH.EngineProxy>.None;
                    return proxy;
                }
            }
            else {
                return Rust.OptionBox<CH.EngineProxy>.None;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static CH.ScreenId OnScreenInit(
            CH.EngineId engineId,
            u64 state,
            Rust.Box<CH.Screen> screen,
            CH.ScreenInfo* info)
        {
            return _engines[engineId].OnScreenInit(state, screen, *info);
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
            _engines[engineId].OnCleared(id);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static bool EventRedrawRequested(CH.EngineId engineId, CH.ScreenId id)
        {
            return _engines[engineId].OnRedrawRequested(id);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void EventResized(CH.EngineId engineId, CH.ScreenId id, u32 width, u32 height)
        {
            _engines[engineId].OnResized(id, width, height);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void EventKeyboard(CH.EngineId engineId, CH.ScreenId id, CH.KeyCode key, bool pressed)
        {
            _engines[engineId].OnKeyboardInput(id, key, pressed);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void EventCharReceived(CH.EngineId engineId, CH.ScreenId id, Rune input)
        {
            _engines[engineId].OnCharReceived(id, input);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void EventMouseButton(CH.EngineId engineId, CH.ScreenId id, CH.MouseButton button, bool pressed)
        {
            _engines[engineId].OnMouseButton(id, button, pressed);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void EventIme(CH.EngineId engineId, CH.ScreenId id, CH.ImeInputData* input)
        {
            _engines[engineId].OnImeInput(id, in *input);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void EventWheel(CH.EngineId engineId, CH.ScreenId id, f32 x_delta, f32 y_delta)
        {
            _engines[engineId].OnWheel(id, x_delta, y_delta);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void EventCursorMoved(CH.EngineId engineId, CH.ScreenId id, f32 x, f32 y)
        {
            _engines[engineId].OnCursorMoved(id, x, y);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void EventCursorEnteredLeft(CH.EngineId engineId, CH.ScreenId id, bool entered)
        {
            _engines[engineId].OnCursorEnteredLeft(id, entered);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void EventClosing(CH.EngineId engineId, CH.ScreenId id, bool* mut_cancel)
        {
            ref bool cancel = ref *mut_cancel;
            _engines[engineId].OnClosing(id, ref cancel);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static Rust.OptionBox<CH.Screen> EventClosed(CH.EngineId engineId, CH.ScreenId id)
        {
            return _engines[engineId].OnClosed(id);
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

    private unsafe CH.ScreenId OnScreenInit(u64 state, Rust.Box<CH.Screen> screenHandle, CH.ScreenInfo info)
    {
        if(_callbacks.TryRemove(state, out var onStart) == false) {
            onStart = (_) => { };
        }
        var mainThread = ThreadId.CurrentThread();
        var syncContextReceiver = (AsyncOperationManager.SynchronizationContext as HikariSynchronizationContext)?.Receiver;
        var screenId = new CH.ScreenId(screenHandle);
        var screen = new Screen(screenHandle, this, mainThread, onStart, syncContextReceiver, info);
        _screens.Add(screenId, screen);
        return screenId;
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

    private void OnMouseButton(CH.ScreenId id, CH.MouseButton button, bool pressed) => _screens[id].Mouse.OnMouseButton(button, pressed);

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
