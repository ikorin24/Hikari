#nullable enable
using System;

namespace Hikari;

public static class NullableExtensions
{
    public static ref readonly T ValueRef<T>(this ref readonly T? self) where T : struct
    {
        return ref Nullable.GetValueRefOrDefaultRef(in self);
    }
}
