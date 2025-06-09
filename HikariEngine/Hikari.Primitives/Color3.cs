#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hikari
{
    [DebuggerDisplay("{DebugView}")]
    [StructLayout(LayoutKind.Explicit)]
    public partial struct Color3 : IEquatable<Color3>
    {
        [FieldOffset(0)]
        public float R;
        [FieldOffset(4)]
        public float G;
        [FieldOffset(8)]
        public float B;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebugView => $"(R: {R}, G: {G}, B: {B})";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color3(float r, float g, float b) => (R, G, B) = (r, g, b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color3(float value) => (R, G, B) = (value, value, value);

        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out float r, out float g, out float b) => (r, g, b) = (R, G, B);

        public readonly override bool Equals(object? obj) => obj is Color3 color && Equals(color);

        public readonly bool Equals(Color3 other) => (R == other.R) && (G == other.G) && (B == other.B);

        public readonly override int GetHashCode() => HashCode.Combine(R, G, B);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Color4 ToColor4() => new(R, G, B, 1f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Color4 ToColor4(float alpha) => new(R, G, B, alpha);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ColorByte ToColorByte() => new()
        {
            R = (byte)(float.Clamp(R, 0f, 1f) * byte.MaxValue),
            G = (byte)(float.Clamp(G, 0f, 1f) * byte.MaxValue),
            B = (byte)(float.Clamp(B, 0f, 1f) * byte.MaxValue),
            A = byte.MaxValue,
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ColorByte ToColorByte(byte alpha) => new()
        {
            R = (byte)(float.Clamp(R, 0f, 1f) * byte.MaxValue),
            G = (byte)(float.Clamp(G, 0f, 1f) * byte.MaxValue),
            B = (byte)(float.Clamp(B, 0f, 1f) * byte.MaxValue),
            A = alpha,
        };

        public static bool operator ==(in Color3 left, in Color3 right) => left.Equals(right);

        public static bool operator !=(in Color3 left, in Color3 right) => !(left == right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color3 FromHexCode(string hexCode) => FromHexCode(hexCode.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color3 FromHexCode(ReadOnlySpan<char> hexCode) =>
            TryFromHexCode(hexCode, out var color) ? color : throw new FormatException($"Invalid Format: '{hexCode.ToString()}'");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryFromHexCode(string hex, out Color3 color) => TryFromHexCode(hex.AsSpan(), out color);

        public static bool TryFromHexCode(ReadOnlySpan<char> hexCode, out Color3 color)
        {
            if(ColorByte.TryFromHexCode(hexCode, out var colorByte) && colorByte.A == byte.MaxValue) {
                color = colorByte.ToColor3();
                return true;
            }
            color = default;
            return false;
        }

        /// <summary>Try to get color from web color name, which must be small letter.</summary>
        /// <param name="name">web color name</param>
        /// <param name="color">color</param>
        /// <returns>success or not</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryFromWebColorName(string name, out Color3 color) => WebColors.TryGetColor3(name, out color);

        public static bool TryFromWebColorName(ReadOnlySpan<char> name, out Color3 color) => WebColors.TryGetColor3(name, out color);

        /// <summary>Get color from web color name, which must be small letter.</summary>
        /// <param name="name">web color name</param>
        /// <returns>color</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color3 FromWebColorName(string name) =>
            WebColors.TryGetColor3(name, out var color) ? color :
            throw new ArgumentException($"Web color name is not defined. The name must be small letter. (name='{name}')");

        public static Color3 FromWebColorName(ReadOnlySpan<char> name) =>
            WebColors.TryGetColor3(name, out var color) ? color :
            throw new ArgumentException($"Web color name is not defined. The name must be small letter. (name='{name.ToString()}')");

        public static bool IsWebColorDefined(string name) => WebColors.IsDefined(name);

        public static bool IsWebColorDefined(ReadOnlySpan<char> name) => WebColors.IsDefined(name);
    }
}
