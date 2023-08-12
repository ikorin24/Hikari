#nullable enable
using System;

namespace Elffy.UI;

internal static class UIShaderSource
{
    public static ReadOnlySpan<byte> TypeDef => """
        struct Vin {
            @location(0) pos: vec3<f32>,
            @location(1) uv: vec2<f32>,
        }
        struct V2F {
            @builtin(position) clip_pos: vec4<f32>,
            @location(0) uv: vec2<f32>,
        }
        struct ScreenInfo {
            size: vec2<u32>,
        }
        struct BufferData
        {
            mvp: mat4x4<f32>,
            solid_color: vec4<f32>,
            rect: vec4<f32>,            // (x, y, width, height)
            border_width: vec4<f32>,    // (top, right, bottom, left)
            border_radius: vec4<f32>,   // (top-left, top-right, bottom-right, bottom-left)
            border_solid_color: vec4<f32>,
        }
        """u8;

    public static ReadOnlySpan<byte> ConstDef => """
        const PI: f32 = 3.141592653589793;
        const INV_PI: f32 = 0.3183098861837907;
        """u8;

    public static ReadOnlySpan<byte> Group0 => """
        @group(0) @binding(0) var<uniform> screen: ScreenInfo;
        @group(0) @binding(1) var<uniform> data: BufferData;
        """u8;

    public static ReadOnlySpan<byte> Fn_pow_x2 => """
        fn pow_x2(x: f32) -> f32 {
            return x * x;
        }
        """u8;

    public static ReadOnlySpan<byte> Fn_blend => """
        fn blend(src: vec4<f32>, dst: vec4<f32>, x: f32) -> vec4<f32> {
            let a = src.a * x;
            return vec4(
                src.rgb * a + (1.0 - a) * dst.rgb,
                a + (1.0 - a) * dst.a,
            );
        }
        """u8;

    public static ReadOnlySpan<byte> Fn_vs_main => """
        @vertex fn vs_main(
            v: Vin,
        ) -> V2F {
            var o: V2F;
            o.clip_pos = data.mvp * vec4<f32>(v.pos, 1.0);
            o.uv = v.uv;
            return o;
        }
        """u8;
}
