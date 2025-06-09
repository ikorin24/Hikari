#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Hikari;

internal static class ThrowHelper
{
    [DoesNotReturn]
    [DebuggerHidden]
    public static void ThrowArgOutOfRange(string paramName) => throw new ArgumentOutOfRangeException(paramName);
}
