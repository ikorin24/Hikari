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

    public static ReadOnlySpan<byte> Fn_corner_area_color => """
        fn corner_area_color(
            f_pos: vec2<f32>, 
            center: vec2<f32>, 
            b_radius: f32, 
            bw: vec2<f32>, 
            back_color: vec4<f32>,
            b_color: vec4<f32>,
        ) -> vec4<f32> {
            let d: vec2<f32> = f_pos + vec2<f32>(0.5, 0.5) - center;
            let len_d = length(d);
            let a = saturate(b_radius - len_d + 0.5);
            if(a <= 0.0) {
                discard;
            }
            if(bw.x == 0.0 && bw.y == 0.0) {
                // To avoid a slight border due to float errors, I don't draw the border color.
                return vec4<f32>(back_color.rgb, back_color.a * a);
            }
            var er_x: f32 = max(0.001, b_radius - bw.x);   // x-axis radius of ellipse
            var er_y: f32 = max(0.001, b_radius - bw.y);   // y-axis radius of ellipse
            // vector from center of ellipse to the crossed point of 'd' and the ellipse
            let v: vec2<f32> = d * er_x * er_y / sqrt(pow_x2(er_y * d.x) + pow_x2(er_x * d.y));
            let b = saturate(len_d - length(v) + 0.5);
            let b_color_blend = blend(b_color, back_color, b);
            return vec4<f32>(b_color_blend.rgb, b_color_blend.a * a);
        }
        """u8;

    public static ReadOnlySpan<byte> Fn_ui_color_shared_algorithm => """
        fn ui_color_shared_algorithm(
            f: V2F,
        ) -> vec4<f32> {
            // pixel coordinates, which is not normalized
            let f_pos: vec2<f32> = floor(f.clip_pos.xy);
            let pos: vec2<f32> = floor(data.rect.xy);
            let size: vec2<f32> = floor(data.rect.zw);
            var back_color: vec4<f32> = calc_back_color(f_pos, pos, size);
            let b_color: vec4<f32> = data.border_solid_color;
            let b_radius = array<f32, 4>(
                floor(data.border_radius.x),
                floor(data.border_radius.y),
                floor(data.border_radius.z),
                floor(data.border_radius.w),
            );
            let b_width = array<f32, 4>(
                floor(data.border_width.x),
                floor(data.border_width.y),
                floor(data.border_width.z),
                floor(data.border_width.w),
            );
            let center = array<vec2<f32>, 4>(
                pos + vec2<f32>(b_radius[0], b_radius[0]),
                pos + vec2<f32>(size.x - b_radius[1], b_radius[1]),
                pos + vec2<f32>(size.x - b_radius[2], size.y - b_radius[2]),
                pos + vec2<f32>(b_radius[3], size.y - b_radius[3]),
            );
            // outside of the rectangle
            if(f_pos.x < pos.x || f_pos.x >= pos.x + size.x || f_pos.y < pos.y || f_pos.y >= pos.y + size.y) {
                discard;
            }

            // top-left corner
            if(f_pos.x < center[0].x && f_pos.y < center[0].y) {
                return corner_area_color(
                    f_pos, center[0], b_radius[0],
                    vec2<f32>(b_width[3], b_width[0]),
                    back_color, b_color,
                );
            }
            // top-right corner
            else if(f_pos.x >= center[1].x && f_pos.y < center[1].y) {
                return corner_area_color(
                    f_pos, center[1], b_radius[1],
                    vec2<f32>(b_width[1], b_width[0]),
                    back_color, b_color,
                );
            }
            // bottom-right corner
            else if(f_pos.x >= center[2].x && f_pos.y >= center[2].y) {
                return corner_area_color(
                    f_pos, center[2], b_radius[2],
                    vec2<f32>(b_width[1], b_width[2]),
                    back_color, b_color,
                );
            }
            // bottom-left corner
            else if(f_pos.x < center[3].x && f_pos.y >= center[3].y) {
                return corner_area_color(
                    f_pos, center[3], b_radius[3],
                    vec2<f32>(b_width[3], b_width[2]),
                    back_color, b_color,
                );
            }
            // side border
            else if(
                f_pos.y < pos.y + b_width[0] || 
                f_pos.x >= pos.x + size.x - b_width[1] || 
                f_pos.y >= pos.y + size.y - b_width[2] || 
                f_pos.x < pos.x + b_width[3]
            ) {
                return b_color;
            }

            return back_color;
        }
        """u8;
}
