#nullable enable
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy;

internal static class AssertExtensions
{
    private static bool IsDebug =>
#if DEBUG
        true;
#else
        false;
#endif

    [DebuggerHidden]
    public static bool WithDebugAssertTrue(
        this bool condition,
        string? message = "The condition is asserted to be `true`, but in fact it is `false`.",
        [CallerArgumentExpression(nameof(condition))] string? callerExpr = null)
    {
        if(IsDebug) {
            Debug.Assert(condition, message, callerExpr);
        }
        return condition;
    }

    [DebuggerHidden]
    public static bool WithDebugAssertFalse(
        this bool condition,
        string? message = "The condition is asserted to be `false`, but in fact it is `true`.",
        [CallerArgumentExpression(nameof(condition))] string? callerExpr = null)
    {
        if(IsDebug) {
            Debug.Assert(condition == false, message, callerExpr);
        }
        return condition;
    }
}
