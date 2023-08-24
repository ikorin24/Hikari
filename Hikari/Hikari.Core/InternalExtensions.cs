#nullable enable
using Elffy.NativeBind;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy;

internal static class InternalExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static CE.Slice<T> AsFixedSlice<T>(this T[] array, PinHandleHolder pins) where T : unmanaged
    {
        return ((ReadOnlyMemory<T>)array).AsFixedSlice(pins);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static CE.Slice<T> AsFixedSlice<T>(this ReadOnlyMemory<T> memory, PinHandleHolder pins) where T : unmanaged
    {
        var handle = memory.Pin();
        pins.Add(handle);
        return new CE.Slice<T>((T*)handle.Pointer, memory.Length);
    }

    public static TResult[] SelectToArray<T, TResult>(this ReadOnlyMemory<T> self, Func<T, TResult> selector)
    {
        var span = self.Span;
        var result = new TResult[span.Length];
        for(int i = 0; i < span.Length; i++) {
            result[i] = selector(span[i]);
        }
        return result;
    }

    public static TResult[] SelectToArray<T, TArg, TResult>(this ReadOnlyMemory<T> self, TArg arg, Func<T, TArg, TResult> selector)
    {
        var span = self.Span;
        var result = new TResult[span.Length];
        for(int i = 0; i < span.Length; i++) {
            result[i] = selector(span[i], arg);
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CE.Opt<TResult> ToNative<T, TResult>(this T? self, Func<T, TResult> mapper) where T : struct where TResult : unmanaged
    {
        return self == null ? CE.Opt<TResult>.None : CE.Opt<TResult>.Some(mapper(self.Value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GetOrThrow<T>(this T? self) where T : struct
    {
        if(self.HasValue == false) {
            Throw();
            [DoesNotReturn] static void Throw() => throw new InvalidOperationException("cannot get value");
        }
        return self.Value;
    }
}
