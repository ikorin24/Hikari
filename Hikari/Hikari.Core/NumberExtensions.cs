#nullable enable
using System.Runtime.CompilerServices;

namespace Hikari;

internal static class NumberExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToInt32(this usize self)
    {
        return checked((int)self);
    }
}
