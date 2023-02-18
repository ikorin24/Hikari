#nullable enable
using Elffy.NativeBind;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Elffy;

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
}
