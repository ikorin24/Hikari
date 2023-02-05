#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy;

internal interface IEngineManaged
{
    IHostScreen? Screen { get; }
    bool IsManaged => Screen is not null;
}

internal static class EngineManagedExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetScreen(this IEngineManaged self, [MaybeNullWhen(false)] out IHostScreen screen)
    {
        screen = self.Screen;
        return screen is not null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IHostScreen GetScreen(this IEngineManaged self)
    {
        var screen = self.Screen;
        if(screen == null) {
            Throw();
            [DoesNotReturn] static void Throw() => throw new InvalidOperationException("Cannot get a host screen");
        }
        return screen;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotEngineManaged(this IEngineManaged self)
    {
        if(self.IsManaged == false) {
            Throw();
            [DoesNotReturn] static void Throw() => throw new InvalidOperationException("The object is not managed by the engine.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfAlreadyEngineManaged(this IEngineManaged self)
    {
        if(self.IsManaged) {
            Throw();
            [DoesNotReturn] static void Throw() => throw new InvalidOperationException("The object is already managed by the engine.");
        }
    }
}
