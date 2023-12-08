#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Hikari;

internal readonly record struct Arc<T> : IDisposable
    where T : class, IArc
{
    private readonly T? _value;

    public T Value => _value!;

    public Arc(T value)
    {
        value.AddRef();
        _value = value;
    }

    public void Dispose()
    {
        _value?.RemoveRef();
    }

    public static implicit operator Arc<T>(T value) => new(value);
}

internal record struct AtomicCounter
{
    private nuint _count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Increment()
    {
        if(IntPtr.Size == 8) {
            Interlocked.Increment(ref Unsafe.As<nuint, ulong>(ref _count));
        }
        else if(IntPtr.Size == 4) {
            Interlocked.Increment(ref Unsafe.As<nuint, uint>(ref _count));
        }
        else {
            Debug.Fail("unexpected environment");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Decrement()
    {
        if(IntPtr.Size == 8) {
            return Interlocked.Decrement(ref Unsafe.As<nuint, ulong>(ref _count)) == 0;
        }
        else if(IntPtr.Size == 4) {
            return Interlocked.Decrement(ref Unsafe.As<nuint, uint>(ref _count)) == 0;
        }
        else {
            Debug.Fail("unexpected environment");
            return false;
        }
    }
}

internal interface IArc
{
    void AddRef();
    void RemoveRef();
}

internal static class ArcExtensions
{
    public static Arc<T> AsArc<T>(this T value)
        where T : class, IArc
    {
        return new(value);
    }
}
