#nullable enable
using System;
using System.Runtime.InteropServices;

namespace Hikari.UI;

internal static class UIShaderSource
{
    [StructLayout(LayoutKind.Sequential, Pack = WgslConst.AlignOf_mat4x4_f32)]
    public readonly record struct BufferData
    {
        public required Matrix4 Mvp { get; init; }
        public required RectF Rect { get; init; }
        public required Vector4 BorderWidth { get; init; }
        public required Vector4 BorderRadius { get; init; }
        public required Color4 BorderSolidColor { get; init; }

        // (offsetX, offsetY, inset_blurRadius, spreadRadius),
        // blurRadius = abs(inset_blurRadius)
        // inset = inset_blurRadius < 0
        public required Vector4 BoxShadowValues { get; init; }
        public required Color4 BoxShadowColor { get; init; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = WgslConst.AlignOf_vec4_f32, Size = 32)]
    public readonly record struct ColorPoint(Color4 Color, float Offset);

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
            rect: vec4<f32>,            // (x, y, width, height)
            border_width: vec4<f32>,    // (top, right, bottom, left)
            border_radius: vec4<f32>,   // (top-left, top-right, bottom-right, bottom-left)
            border_solid_color: vec4<f32>,
            box_shadow_values: vec4<f32>,   // (offsetX, offsetY, inset_blurRadius, spreadRadius),
                                            // blurRadius = abs(inset_blurRadius)
                                            // inset = inset_blurRadius < 0
            box_shadow_color: vec4<f32>,
        }

        struct BrushBufferData
        {
            direction: f32,
            color_count: u32,
            colors: array<ColorPoint>,
        }

        struct ColorPoint
        {
            color: vec4<f32>,
            offset: f32,
        }
        """u8;

    public static ReadOnlySpan<byte> ConstDef => """
        const PI = 3.141592653589793;
        const INV_PI = 0.3183098861837907;
        """u8;

    public static ReadOnlySpan<byte> Group0 => """
        @group(0) @binding(0) var<uniform> screen: ScreenInfo;
        @group(0) @binding(1) var<uniform> data: BufferData;
        """u8;
    public static ReadOnlySpan<byte> Group1 => """
        @group(1) @binding(0) var tex: texture_2d<f32>;
        @group(1) @binding(1) var tex_sampler: sampler;
        """u8;
    public static ReadOnlySpan<byte> Group2 => """
        @group(2) @binding(0) var<storage, read> background: BrushBufferData;
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
                return vec4<f32>(0.0, 0.0, 0.0, 0.0);
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

    public static ReadOnlySpan<byte> Fn_get_texel_color => """
        const TEXT_HALIGN_LEFT: u32 = 0u;
        const TEXT_HALIGN_CENTER: u32 = 1u;
        const TEXT_HALIGN_RIGHT: u32 = 2u;
        const TEXT_VALIGN_TOP: u32 = 0u;
        const TEXT_VALIGN_CENTER: u32 = 1u;
        const TEXT_VALIGN_BOTTOM: u32 = 2u;

        fn get_texel_color(
            f_pos: vec2<f32>, 
            h_align: u32, 
            v_align: u32,
            rect_pos: vec2<f32>,
            rect_size: vec2<f32>,
        ) -> vec4<f32> {
            let tex_size: vec2<i32> = textureDimensions(tex, 0).xy;
            var offset_in_rect: vec2<f32>;
            if(h_align == TEXT_HALIGN_CENTER) {
                offset_in_rect.x = (rect_size.x - vec2<f32>(tex_size).x) * 0.5;
            }
            else if(h_align == TEXT_HALIGN_RIGHT) {
                offset_in_rect.x = rect_size.x - vec2<f32>(tex_size).x;
            }
            else {
                // h_align == TEXT_HALIGN_LEFT
                offset_in_rect.x = 0.0;
            }

            if(v_align == TEXT_VALIGN_CENTER) {
                offset_in_rect.y = (rect_size.y - vec2<f32>(tex_size).y) * 0.5;
            }
            else if(v_align == TEXT_VALIGN_TOP) {
                offset_in_rect.y = 0.0;
            }
            else {
                // v_align == TEXT_VALIGN_BOTTOM
                offset_in_rect.y = rect_size.y - vec2<f32>(tex_size).y;
            }
            let texel_pos: vec2<f32> = f_pos - (rect_pos + offset_in_rect);
            if(texel_pos.x < 0.0 || texel_pos.x >= f32(tex_size.x) || texel_pos.y < 0.0 || texel_pos.y >= f32(tex_size.y)) {
                return vec4<f32>(0.0, 0.0, 0.0, 0.0);
            }
            else {
                return textureLoad(tex, vec2<i32>(texel_pos), 0);
            }
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
            let back_color: vec4<f32> = calc_back_color(f_pos, pos, size);
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

            var color: vec4<f32>;
            var s_color = vec4<f32>(0.0, 0.0, 0.0, 0.0);
            let s_width: f32 = data.box_shadow_values.z + data.box_shadow_values.w;
            let s_pos: vec2<f32> = pos - vec2<f32>(s_width, s_width) + data.box_shadow_values.xy;
            let s_size: vec2<f32> = size + vec2<f32>(s_width, s_width) * 2.0;
            // inside of the shadow rectangle
            if(
                f_pos.x >= s_pos.x &&
                f_pos.x < s_pos.x + s_size.x &&
                f_pos.y >= s_pos.y &&
                f_pos.y < s_pos.y + s_size.y
            ) {
                // center of shadow corner
                let s_center = array<vec2<f32>, 4>(
                    center[0] + data.box_shadow_values.xy,
                    center[1] + data.box_shadow_values.xy,
                    center[2] + data.box_shadow_values.xy,
                    center[3] + data.box_shadow_values.xy,
                );
                let s_radius = array<f32, 4>(
                    b_radius[0] + data.box_shadow_values.w,
                    b_radius[1] + data.box_shadow_values.w,
                    b_radius[2] + data.box_shadow_values.w,
                    b_radius[3] + data.box_shadow_values.w,
                );
                let r = abs(data.box_shadow_values.z) + 0.5;
                // shadow top-left corner
                if(f_pos.x < s_center[0].x && f_pos.y < s_center[0].y) {
                    let d: vec2<f32> = f_pos + vec2<f32>(0.5, 0.5) - s_center[0];
                    let x = clamp((length(d) - 0.5) - s_radius[0], -r, r);
                    s_color = vec4<f32>(
                        data.box_shadow_color.rgb,
                        data.box_shadow_color.a * (0.5 - 0.5 * sin(x * PI * 0.5 / r)),
                    );
                }
                // shadow top-right corner
                else if(f_pos.x >= s_center[1].x && f_pos.y < s_center[1].y) {
                    let d: vec2<f32> = f_pos + vec2<f32>(0.5, 0.5) - s_center[1];
                    let x = clamp((length(d) - 0.5) - s_radius[1], -r, r);
                    s_color = vec4<f32>(
                        data.box_shadow_color.rgb,
                        data.box_shadow_color.a * (0.5 - 0.5 * sin(x * PI * 0.5 / r)),
                    );
                }
                // shadow bottom-right corner
                else if(f_pos.x >= s_center[2].x && f_pos.y >= s_center[2].y) {
                    let d: vec2<f32> = f_pos + vec2<f32>(0.5, 0.5) - s_center[2];
                    let x = clamp((length(d) - 0.5) - s_radius[2], -r, r);
                    s_color = vec4<f32>(
                        data.box_shadow_color.rgb,
                        data.box_shadow_color.a * (0.5 - 0.5 * sin(x * PI * 0.5 / r)),
                    );
                }
                // shadow bottom-left corner
                else if(f_pos.x < s_center[3].x && f_pos.y >= s_center[3].y) {
                    let d: vec2<f32> = f_pos + vec2<f32>(0.5, 0.5) - s_center[3];
                    let x = clamp((length(d) - 0.5) - s_radius[3], -r, r);
                    s_color = vec4<f32>(
                        data.box_shadow_color.rgb,
                        data.box_shadow_color.a * (0.5 - 0.5 * sin(x * PI * 0.5 / r)),
                    );
                }
                else {
                    s_color = data.box_shadow_color;
                }
            }

            // inside of the rectangle
            if(
                f_pos.x >= pos.x && 
                f_pos.x < pos.x + size.x &&
                f_pos.y >= pos.y &&
                f_pos.y < pos.y + size.y
            ) {
                // top-left corner
                if(f_pos.x < center[0].x && f_pos.y < center[0].y) {
                    let tmp = corner_area_color(
                        f_pos, center[0], b_radius[0],
                        vec2<f32>(b_width[3], b_width[0]),
                        back_color, b_color,
                    );
                    color = blend(tmp, s_color, 1.0);
                }
                // top-right corner
                else if(f_pos.x >= center[1].x && f_pos.y < center[1].y) {
                    let tmp = corner_area_color(
                        f_pos, center[1], b_radius[1],
                        vec2<f32>(b_width[1], b_width[0]),
                        back_color, b_color,
                    );
                    color = blend(tmp, s_color, 1.0);
                }
                // bottom-right corner
                else if(f_pos.x >= center[2].x && f_pos.y >= center[2].y) {
                    let tmp = corner_area_color(
                        f_pos, center[2], b_radius[2],
                        vec2<f32>(b_width[1], b_width[2]),
                        back_color, b_color,
                    );
                    color = blend(tmp, s_color, 1.0);
                }
                // bottom-left corner
                else if(f_pos.x < center[3].x && f_pos.y >= center[3].y) {
                    let tmp = corner_area_color(
                        f_pos, center[3], b_radius[3],
                        vec2<f32>(b_width[3], b_width[2]),
                        back_color, b_color,
                    );
                    color = blend(tmp, s_color, 1.0);
                }
                // side border
                else if(
                    f_pos.y < pos.y + b_width[0] || 
                    f_pos.x >= pos.x + size.x - b_width[1] || 
                    f_pos.y >= pos.y + size.y - b_width[2] || 
                    f_pos.x < pos.x + b_width[3]
                ) {
                    color = blend(b_color, s_color, 1.0);
                }
                else {
                    color = blend(back_color, s_color, 1.0);
                }
            }
            // outside of the rectangle
            else {
                color = s_color;
            }

            if(color.a < 0.001) {
                discard;
            }
            return color;
        }
        """u8;

    public static ReadOnlySpan<byte> Fn_calc_background_brush_color => """
        fn calc_background_brush_color(
            f_pos: vec2<f32>,
            pos: vec2<f32>,
            size: vec2<f32>,
        ) -> vec4<f32> {
            var point0: vec2<f32>;
            var point1: vec2<f32>;
            let dir = background.direction;
            if(background.direction < PI * 0.5) {
                point0 = vec2<f32>(0.0, f32(size.y));
                point1 = vec2<f32>(f32(size.x), 0.0);
            } else if(background.direction < PI) {
                point0 = vec2<f32>(0.0, 0.0);
                point1 = size;
            } else if(background.direction < PI * 1.5) {
                point0 = vec2<f32>(f32(size.x), 0.0);
                point1 = vec2<f32>(0.0, f32(size.y));
            } else {
                point0 = size;
                point1 = vec2<f32>(0.0, 0.0);
            }
            let n = vec2<f32>(sin(dir), -cos(dir));
            let x: vec2<f32> = f_pos - pos;
            let a: f32 = abs(dot(n, x) - dot(n, point0)) / abs(dot(n, point1) - dot(n, point0));

            var j0: f32 = 0.0;
            for(var i = 0u; i < background.color_count; i++) {
                let ok: f32 = step(background.colors[i].offset, a);
                j0 = j0 * (1.0 - ok) + f32(i) * ok;
            }
            let i0: u32 = u32(j0);
            let i1 = min(i0 + 1u, background.color_count - 1u);
            let o0: f32 = background.colors[i0].offset;
            let o1: f32 = background.colors[i1].offset;
            let mixed_color = mix(
                background.colors[i0].color,
                background.colors[i1].color,
                saturate((a - o0) / (o1 - o0 + 0.0001)),
            );
            return mixed_color;
        }
        """u8;

    public static ReadOnlySpan<byte> Fn_gamma22 => """
        fn gamma22(color: vec4<f32>) -> vec4<f32> {
            return vec4<f32>(
                pow(color.rgb, vec3<f32>(2.2, 2.2, 2.2)),
                color.a,
            );
        }
        """u8;

    public static ReadOnlySpan<byte> Fn_degamma22 => """
        fn degamma22(color: vec4<f32>) -> vec4<f32> {
            return vec4<f32>(
                pow(color.rgb, vec3<f32>(
                    0.454545454,
                    0.454545454,
                    0.454545454,
                )),
                color.a,
            );
        }
        """u8;
}
