#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Elffy;

internal static class ThrowHelper
{
    [DebuggerHidden]
    [DoesNotReturn]
    public static void ThrowInvalidOperation(string message) => throw new InvalidOperationException(message);
}
