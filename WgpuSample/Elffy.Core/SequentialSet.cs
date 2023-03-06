//#nullable enable
//namespace EnumMapping;

//[global::System.Diagnostics.Conditional("COMPILE_TIME_ONLY")]
//[global::System.AttributeUsage(global::System.AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
//internal sealed class SequentialSetAttribute : global::System.Attribute
//{
//    public SequentialSetAttribute() { }
//    public SequentialSetAttribute(string constFieldName) { }
//}

//internal static class SequentialSet<T> where T : unmanaged, global::System.Enum
//{
//    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
//    public static int Count()
//    {
//        if(TryGetCount(out var count) == false) {
//            ThrowInvalidEnumType();
//        }
//        return count;
//    }

//    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
//    public static bool TryGetCount(out int count)
//    {
//        if(typeof(T) == typeof(global::Elffy.Keys)) {
//            count = SequentialSetCount.Keys;
//            return true;
//        }
//        count = 0;
//        return false;
//    }

//    [global::System.Diagnostics.CodeAnalysis.DoesNotReturn]
//    private static void ThrowInvalidEnumType() => throw new global::System.InvalidOperationException($"'{typeof(T).FullName}' is invalid type for this operation.");
//}

//internal sealed class SequentialSetCount
//{
//    public const int Keys = 163;
//}
