#nullable enable
using System.Runtime.CompilerServices;

namespace Hikari;

partial struct Matrix4
{
    /// <summary>
    /// Depth range is [0, 1]. 0 is far, 1 is near.
    /// </summary>
    public static class ReversedZ
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4 OrthographicProjection(float left, float right, float bottom, float top, float depthNear, float depthFar)
        {
            var mat = DefaultZ.OrthographicProjection(left, right, bottom, top, depthNear, depthFar);
            mat.M20 = -mat.M20 + mat.M30;
            mat.M21 = -mat.M21 + mat.M31;
            mat.M22 = -mat.M22 + mat.M32;
            mat.M23 = -mat.M23 + mat.M33;
            return mat;

            //return RemapRangeMatrix * DefaultZ.OrthographicProjection(left, right, bottom, top, depthNear, depthFar);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4 PerspectiveProjection(float left, float right, float bottom, float top, float depthNear, float depthFar)
        {
            var mat = DefaultZ.PerspectiveProjection(left, right, bottom, top, depthNear, depthFar);
            mat.M20 = -mat.M20 + mat.M30;
            mat.M21 = -mat.M21 + mat.M31;
            mat.M22 = -mat.M22 + mat.M32;
            mat.M23 = -mat.M23 + mat.M33;
            return mat;

            //return RemapRangeMatrix * DefaultZ.PerspectiveProjection(left, right, bottom, top, depthNear, depthFar);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4 PerspectiveProjection(float fovy, float aspect, float depthNear, float depthFar)
        {
            var mat = DefaultZ.PerspectiveProjection(fovy, aspect, depthNear, depthFar);
            mat.M20 = -mat.M20 + mat.M30;
            mat.M21 = -mat.M21 + mat.M31;
            mat.M22 = -mat.M22 + mat.M32;
            mat.M23 = -mat.M23 + mat.M33;
            return mat;

            //return RemapRangeMatrix * DefaultZ.PerspectiveProjection(fovy, aspect, depthNear, depthFar);
        }

        //private static Matrix4 RemapRangeMatrix => new Matrix4(
        //        new Vector4(1, 0, 0, 0),
        //        new Vector4(0, 1, 0, 0),
        //        new Vector4(0, 0, -1, 0),
        //        new Vector4(0, 0, 1, 1));
    }
}
