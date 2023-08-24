#nullable enable
using Hikari.NativeBind;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Hikari;

internal unsafe static class InterlockedEx
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rust.Box<T> Exchange<T>(ref Rust.Box<T> location1, Rust.Box<T> value) where T : INativeTypeNonReprC
    {
        var exchanged = Interlocked.Exchange(
            ref Unsafe.As<Rust.Box<T>, usize>(ref location1),
            Unsafe.As<Rust.Box<T>, usize>(ref value));
        return Unsafe.As<usize, Rust.Box<T>>(ref exchanged);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rust.OptionBox<T> Exchange<T>(ref Rust.OptionBox<T> location1, Rust.OptionBox<T> value) where T : INativeTypeNonReprC
    {
        var exchanged = Interlocked.Exchange(
            ref Unsafe.As<Rust.OptionBox<T>, usize>(ref location1),
            Unsafe.As<Rust.OptionBox<T>, usize>(ref value));
        return Unsafe.As<usize, Rust.OptionBox<T>>(ref exchanged);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LifeState CompareExchange(ref LifeState location1, LifeState value, LifeState comparand)
    {
        {
#pragma warning disable CS0219 // unused variable
            // [compile-time assertion]
            // sizeof(LifeState) == sizeof(uint)
            const uint _ = checked((uint)(sizeof(uint) - sizeof(LifeState)));
            const uint __ = checked((uint)(sizeof(LifeState) - sizeof(uint)));
#pragma warning restore CS0219 // unused variable
        }

        var exchanged = Interlocked.CompareExchange(
            ref Unsafe.As<LifeState, uint>(ref location1),
            (uint)value, (uint)comparand);
        return Unsafe.As<uint, LifeState>(ref exchanged);
    }
}
