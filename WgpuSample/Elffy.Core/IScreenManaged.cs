#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy;

internal interface IScreenManaged
{
    Screen Screen { get; }
    bool IsManaged { get; }
}

internal static class EngineManagedExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void ThrowIfNotScreenManaged(this IScreenManaged self)
    {
        if(self.IsManaged == false) {
            Throw();
            [DoesNotReturn]
            [DebuggerHidden]
            static void Throw() => throw new InvalidOperationException("The object is not managed by the engine.");
        }
    }
}
