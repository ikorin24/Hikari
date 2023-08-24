#nullable enable
using System.Runtime.CompilerServices;

namespace Hikari
{
    public static class NullableExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetValue<T>(this T? input, out T value) where T : struct
        {
            if(input.HasValue) {
                value = input.Value;
                return true;
            }
            value = default;
            return false;
        }
    }
}
