#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Hikari;

public interface IScreenManaged
{
    Screen Screen { get; }
    bool IsManaged { get; }

    void Validate();

    public static void DefaultValidate(IScreenManaged self)
    {
        if(self.IsManaged == false) {
            Throw(self.GetType().FullName);

            [DoesNotReturn]
            [DebuggerHidden]
            static void Throw(string? typeName) => throw new InvalidOperationException($"The object is not managed by the engine. Type: {typeName}");
        }
    }
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
