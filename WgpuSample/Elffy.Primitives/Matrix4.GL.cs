#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace Elffy;

partial struct Matrix4
{
    public static class GL
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4 OrthographicProjection(float left, float right, float bottom, float top, float depthNear, float depthFar)
        {
            var invRL = 1.0f / (right - left);
            var invTB = 1.0f / (top - bottom);
            var invFN = 1.0f / (depthFar - depthNear);

            return new Matrix4(
                2 * invRL, 0, 0, -(right + left) * invRL,
                0, 2 * invTB, 0, -(top + bottom) * invTB,
                0, 0, -2 * invFN, -(depthFar + depthNear) * invFN,
                0, 0, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4 PerspectiveProjection(float left, float right, float bottom, float top, float depthNear, float depthFar)
        {
            if(depthNear <= 0) { throw new ArgumentOutOfRangeException(nameof(depthNear)); }
            if(depthFar <= 0) { throw new ArgumentOutOfRangeException(nameof(depthFar)); }
            if(depthNear >= depthFar) { throw new ArgumentOutOfRangeException(nameof(depthNear)); }

            var x = 2.0f * depthNear / (right - left);
            var y = 2.0f * depthNear / (top - bottom);
            var a = (right + left) / (right - left);
            var b = (top + bottom) / (top - bottom);
            var c = -(depthFar + depthNear) / (depthFar - depthNear);
            var d = -(2.0f * depthFar * depthNear) / (depthFar - depthNear);

            return new Matrix4(
                x, 0, a, 0,
                0, y, b, 0,
                0, 0, c, d,
                0, 0, -1, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4 PerspectiveProjection(float fovy, float aspect, float depthNear, float depthFar)
        {
            if(fovy <= 0 || fovy > float.Pi) { throw new ArgumentOutOfRangeException(nameof(fovy)); }
            if(aspect <= 0) { throw new ArgumentOutOfRangeException(nameof(aspect)); }
            if(depthNear <= 0) { throw new ArgumentOutOfRangeException(nameof(depthNear)); }
            if(depthFar <= 0) { throw new ArgumentOutOfRangeException(nameof(depthFar)); }

            var maxY = depthNear * MathF.Tan(0.5f * fovy);
            var minY = -maxY;
            var minX = minY * aspect;
            var maxX = maxY * aspect;

            return PerspectiveProjection(minX, maxX, minY, maxY, depthNear, depthFar);
        }

        public static Matrix4 GLToWebGpu => new Matrix4(
            new Vector4(1, 0, 0, 0),
            new Vector4(0, 1, 0, 0),
            new Vector4(0, 0, 0.5f, 0),
            new Vector4(0, 0, 0.5f, 1));
    }
}
