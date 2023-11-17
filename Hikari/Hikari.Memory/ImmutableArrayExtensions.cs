#nullable enable
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hikari;

public static class ImmutableArrayExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImmutableArray<T> AsImmutableArray<T>(this T[]? self)
    {
        return ImmutableCollectionsMarshal.AsImmutableArray(self);
    }
}
