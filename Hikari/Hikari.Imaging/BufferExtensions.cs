﻿#nullable enable
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Buffers.Binary;

namespace Hikari.Imaging
{
    internal static class BufferExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint UInt32BigEndian(this Span<byte> buffer)
        {
            return BinaryPrimitives.ReadUInt32BigEndian(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint UInt32BigEndian(this ReadOnlySpan<byte> buffer)
        {
            return BinaryPrimitives.ReadUInt32BigEndian(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Int32BigEndian(this Span<byte> buffer)
        {
            return BinaryPrimitives.ReadInt32BigEndian(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Int32BigEndian(this ReadOnlySpan<byte> buffer)
        {
            return BinaryPrimitives.ReadInt32BigEndian(buffer);
        }
    }
}
