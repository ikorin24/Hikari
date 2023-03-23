#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy;

internal interface IEngineManaged
{
    HostScreen Screen { get; }
    bool IsManaged { get; }
}

internal static class EngineManagedExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotEngineManaged(this IEngineManaged self)
    {
        if(self.IsManaged == false) {
            Throw();
            [DoesNotReturn] static void Throw() => throw new InvalidOperationException("The object is not managed by the engine.");
        }
    }
}
