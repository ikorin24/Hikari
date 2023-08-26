#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Hikari
{
    [DebuggerDisplay("{DebugDisplay}")]
    [StructLayout(LayoutKind.Explicit)]
    public struct RectF : IEquatable<RectF>
    {
        [FieldOffset(0)]
        public float X;
        [FieldOffset(4)]
        public float Y;
        [FieldOffset(8)]
        public float Width;
        [FieldOffset(12)]
        public float Height;

        public readonly Vector2 Position => new Vector2(X, Y);
        public readonly Vector2 Size => new Vector2(Width, Height);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebugDisplay => $"({X}, {Y}, {Width}, {Height})";

        public RectF(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public RectF(in Vector2 pos, in Vector2 size)
        {
            X = pos.X;
            Y = pos.Y;
            Width = size.X;
            Height = size.Y;
        }

        public readonly bool Contains(Vector2 pos)
        {
            return (X <= pos.X) && (pos.X < X + Width) &&
                   (Y <= pos.Y) && (pos.Y < Y + Height);
        }

        public readonly RectF GetMargedRect(in RectF rect)
        {
            var minX = float.Min(X, rect.X);
            var minY = float.Min(Y, rect.Y);
            var maxX = float.Max(X + Width, rect.X + rect.Width);
            var maxY = float.Max(Y + Height, rect.Y + rect.Height);
            return new RectF
            {
                X = minX,
                Y = minY,
                Width = maxX - minX,
                Height = maxY - minY,
            };
        }

        public readonly override bool Equals(object? obj) => obj is RectF f && Equals(f);

        public readonly bool Equals(RectF other) => X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;

        public readonly override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

        public static bool operator ==(RectF left, RectF right) => left.Equals(right);

        public static bool operator !=(RectF left, RectF right) => !(left == right);
    }
}
