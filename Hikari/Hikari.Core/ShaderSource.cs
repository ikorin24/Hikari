#nullable enable
using System;

namespace Hikari;

internal static class ShaderSource
{
    public static ReadOnlySpan<byte> Fn_Inverse2x2 => """
        fn Inverse2x2(m: mat2x2<f32>) -> mat2x2<f32> {
            let inv_det_m: f32 = 1.0 / determinant(m);
            return mat2x2<f32>(
                m[1].y * inv_det_m,    // [0][0]
                -m[0].y * inv_det_m,    // [0][1]

                -m[1].x * inv_det_m,    // [1][0]
                m[0].x * inv_det_m     // [1][1]
            );
        }
        """u8;

    public static ReadOnlySpan<byte> Fn_Inverse3x3 => """
        fn Inverse3x3(m: mat3x3<f32>) -> mat3x3<f32> {
            let inv_det_m: f32 = 1.0 / determinant(m);
            return mat3x3<f32>(
                 determinant(mat2x2<f32>(m[1].yz, m[2].yz)) * inv_det_m,    // [0][0]
                -determinant(mat2x2<f32>(m[0].yz, m[2].yz)) * inv_det_m,    // [0][1]
                 determinant(mat2x2<f32>(m[0].yz, m[1].yz)) * inv_det_m,    // [0][2]

                -determinant(mat2x2<f32>(m[1].xz, m[2].xz)) * inv_det_m,    // [1][0]
                 determinant(mat2x2<f32>(m[0].xz, m[2].xz)) * inv_det_m,    // [1][1]
                -determinant(mat2x2<f32>(m[0].xz, m[1].xz)) * inv_det_m,    // [1][2]

                 determinant(mat2x2<f32>(m[1].xy, m[2].xy)) * inv_det_m,    // [2][0]
                -determinant(mat2x2<f32>(m[0].xy, m[2].xy)) * inv_det_m,    // [2][1]
                 determinant(mat2x2<f32>(m[0].xy, m[1].xy)) * inv_det_m,    // [2][2]
            );
        }
        """u8;

    public static ReadOnlySpan<byte> Fn_Inverse4x4 => """
        fn Inverse4x4(m: mat4x4<f32>) -> mat4x4<f32> {
            let inv_det_m: f32 = 1.0 / determinant(m);
            return mat4x4<f32>(
                 determinant(mat3x3<f32>(m[1].yzw, m[2].yzw, m[3].yzw)) * inv_det_m,    // [0][0]
                -determinant(mat3x3<f32>(m[0].yzw, m[2].yzw, m[3].yzw)) * inv_det_m,    // [0][1]
                 determinant(mat3x3<f32>(m[0].yzw, m[1].yzw, m[3].yzw)) * inv_det_m,    // [0][2]
                -determinant(mat3x3<f32>(m[0].yzw, m[1].yzw, m[2].yzw)) * inv_det_m,    // [0][3]

                -determinant(mat3x3<f32>(m[1].xzw, m[2].xzw, m[3].xzw)) * inv_det_m,    // [1][0]
                 determinant(mat3x3<f32>(m[0].xzw, m[2].xzw, m[3].xzw)) * inv_det_m,    // [1][1]
                -determinant(mat3x3<f32>(m[0].xzw, m[1].xzw, m[3].xzw)) * inv_det_m,    // [1][2]
                 determinant(mat3x3<f32>(m[0].xzw, m[1].xzw, m[2].xzw)) * inv_det_m,    // [1][3]

                 determinant(mat3x3<f32>(m[1].xyw, m[2].xyw, m[3].xyw)) * inv_det_m,    // [2][0]
                -determinant(mat3x3<f32>(m[0].xyw, m[2].xyw, m[3].xyw)) * inv_det_m,    // [2][1]
                 determinant(mat3x3<f32>(m[0].xyw, m[1].xyw, m[3].xyw)) * inv_det_m,    // [2][2]
                -determinant(mat3x3<f32>(m[0].xyw, m[1].xyw, m[2].xyw)) * inv_det_m,    // [2][3]

                -determinant(mat3x3<f32>(m[1].xyz, m[2].xyz, m[3].xyz)) * inv_det_m,    // [3][0]
                 determinant(mat3x3<f32>(m[0].xyz, m[2].xyz, m[3].xyz)) * inv_det_m,    // [3][1]
                -determinant(mat3x3<f32>(m[0].xyz, m[1].xyz, m[3].xyz)) * inv_det_m,    // [3][2]
                 determinant(mat3x3<f32>(m[0].xyz, m[1].xyz, m[2].xyz)) * inv_det_m,    // [3][3]
            );
        }
        """u8;
}
