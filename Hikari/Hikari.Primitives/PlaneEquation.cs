#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Hikari
{
    [DebuggerDisplay("{DebuggerDisplay}")]
    public readonly struct PlaneEquation : IEquatable<PlaneEquation>
    {
        // [equation of plane]
        // nx*x + ny*y + nz*z + d = 0

        // Don't change layout of fileds. It must have same layout as Vector4 for SIMD.
        public readonly Vector3 Normal; // normalized
        public readonly float D;

        private string DebuggerDisplay
        {
            get
            {
                var d = (D < 0) ? $"- {-D}" : $"+ {D}";
                return $"plane: {Normal.X}X + {Normal.Y}Y + {Normal.Z}Z {d} = 0";
            }
        }

        private PlaneEquation(in Vector3 normal, float d)
        {
            Normal = normal;
            D = d;
        }

        public static PlaneEquation FromTriangle(in Vector3 p0, in Vector3 p1, in Vector3 p2)
        {
            var n = Vector3.Cross(p2 - p1, p0 - p1).Normalized();
            return new PlaneEquation(n, -n.Dot(p1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsLineCrossing(in Vector3 from, in Vector3 to)
        {
            return (Normal.Dot(from) + D >= 0) ^ (Normal.Dot(to) + D >= 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAbove(in Vector3 pos) => Normal.Dot(pos) + D >= 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetSignedDistance(in Vector3 pos) => Normal.Dot(pos) + D;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetAbsDistance(in Vector3 pos) => MathF.Abs(Normal.Dot(pos) + D);

        public override bool Equals(object? obj) => obj is PlaneEquation equation && Equals(equation);

        public bool Equals(PlaneEquation other) => Normal.Equals(other.Normal) && D == other.D;

        public override int GetHashCode() => HashCode.Combine(Normal, D);

        public static bool operator ==(PlaneEquation left, PlaneEquation right) => left.Equals(right);

        public static bool operator !=(PlaneEquation left, PlaneEquation right) => !(left == right);
    }
}
