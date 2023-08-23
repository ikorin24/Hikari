#nullable enable
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Elffy.Mathematics
{
    public static class MathTool
    {
        /// <summary>Convert degree to radian</summary>
        /// <param name="degree">degree value</param>
        /// <returns>radian value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToRadian(this int degree) => ToRadian((float)degree);

        /// <summary>Convert degree to radian</summary>
        /// <param name="degree">degree value</param>
        /// <returns>radian value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToRadian(this float degree) => degree * (float.Pi / 180f);

        /// <summary>Convert radian to degree</summary>
        /// <param name="radian">radian value</param>
        /// <returns>degree value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToDegree(this int radian) => (float)radian / (float.Pi / 180f);

        /// <summary>Convert radian to degree</summary>
        /// <param name="radian">radian value</param>
        /// <returns>degree value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToDegree(this float radian) => radian / (float.Pi / 180f);

        /// <summary>Round up value to power of two</summary>
        /// <remarks>[NOTE] 0 or negative value return 1.</remarks>
        /// <param name="value">value to round up</param>
        /// <returns>value of power of two</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RoundUpToPowerOfTwo(int value)
        {
            if(value > (1 << 30)) {
                throw new ArgumentOutOfRangeException("Value is too large to round up to power of two as int.");
            }
            return 1 << (32 - BitOperations.LeadingZeroCount((uint)value - 1));
        }

        /// <summary>Round up value to power of two</summary>
        /// <remarks>[NOTE] 0 returns 1.</remarks>
        /// <param name="value">value to round up</param>
        /// <returns>value of power of two</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint RoundUpToPowerOfTwo(uint value)
        {
            if(value > (1u << 31)) {
                throw new ArgumentOutOfRangeException("Value is too large to round up to power of two as uint.");
            }
            return 1u << (32 - BitOperations.LeadingZeroCount(value - 1));
        }

        /// <summary>Round up value to power of two</summary>
        /// <remarks>[NOTE] 0 or negative value return 1.</remarks>
        /// <param name="value">value to round up</param>
        /// <returns>value of power of two</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long RoundUpToPowerOfTwo(long value)
        {
            if(value > (1L << 62)) {
                throw new ArgumentOutOfRangeException("Value is too large to round up to power of two as long.");
            }
            return 1L << (64 - BitOperations.LeadingZeroCount((ulong)value - 1));
        }

        /// <summary>Round up value to power of two</summary>
        /// <remarks>[NOTE] 0 returns 1.</remarks>
        /// <param name="value">value to round up</param>
        /// <returns>value of power of two</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong RoundUpToPowerOfTwo(ulong value)
        {
            if(value > (1uL << 63)) {
                throw new ArgumentOutOfRangeException("Value is too large to round up to power of two as ulong.");
            }
            return 1uL << (64 - BitOperations.LeadingZeroCount(value - 1));
        }

        /// <summary>Get whether value is power of two or not.</summary>
        /// <remarks>[NOTE] 0 returns true, and all negative values return false.</remarks>
        /// <param name="x">value to check</param>
        /// <returns>value is power of two or not</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOfTwo(int x)
        {
            return (x & (x - 1)) == 0;
        }

        /// <summary>Get whether value is power of two or not.</summary>
        /// <remarks>[NOTE] 0 returns true</remarks>
        /// <param name="x">value to check</param>
        /// <returns>value is power of two or not</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOfTwo(uint x)
        {
            return (x & (x - 1u)) == 0u;
        }

        /// <summary>Get whether value is power of two or not.</summary>
        /// <remarks>[NOTE] 0 returns true, and all negative values return false.</remarks>
        /// <param name="x">value to check</param>
        /// <returns>value is power of two or not</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOfTwo(long x)
        {
            return (x & (x - 1L)) == 0L;
        }

        /// <summary>Get whether value is power of two or not.</summary>
        /// <remarks>[NOTE] 0 returns true</remarks>
        /// <param name="x">value to check</param>
        /// <returns>value is power of two or not</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOfTwo(ulong x)
        {
            return (x & (x - 1uL)) == 0uL;
        }
    }
}
