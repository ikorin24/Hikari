#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.Collections.Concurrent;
using Hikari.NativeBind;

namespace Hikari;

unsafe partial class EngineCore
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void ReadBuffer(
        this Rust.Ref<CH.HostScreen> screen,
        CH.BufferSlice buffer_slice,
        ReadOnlySpanAction<byte> onRead,
        Action<Exception>? onException)
    {
        var token = Callback.NewToken();
        Callback.Register(token, onRead, onException);
        hikari_read_buffer(screen, buffer_slice, token, &OnCallback).Validate();

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static void OnCallback(usize token, ApiResult result, byte* ptr, usize length)
        {
            Action<Exception>? onException = null;
            try {
                if(!Callback.Take(token, out var callback)) {
                    Debug.Fail($"Callback not found. token: {token}");
                }
                (var onRead, onException) = callback;

                result.Validate();
                if(int.MaxValue < length) {
                    throw new NotImplementedException();
                }
                var span = new ReadOnlySpan<byte>(ptr, (int)length);
                onRead.Invoke(span);
            }
            catch(Exception ex) {
                onException?.Invoke(ex);
            }
        }
    }
}

file record struct Callback(ReadOnlySpanAction<byte> OnRead, Action<Exception>? OnException)
{
    private static ulong _token;
    private static readonly ConcurrentDictionary<usize, Callback> _callbacks = new();

    public static usize NewToken() => (usize)Interlocked.Increment(ref _token);

    public static bool Register(usize token, ReadOnlySpanAction<byte> onRead, Action<Exception>? onException)
    {
        return _callbacks.TryAdd(token, new(onRead, onException));
    }

    public static bool Take(usize token, out Callback callback)
    {
        return _callbacks.TryRemove(token, out callback);
    }
}
