#nullable enable
using Hikari;
using Hikari.Collections;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Hikari.UI;

[DebuggerDisplay("{DebugView}")]
public readonly partial struct Brush : IEquatable<Brush>
{
    private readonly BrushType _type;
    private readonly Color4 _solidColor;
    private readonly float _directionRadian;    // [0, 2*PI)
    private readonly GradientStop[] _gradientStops;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebugView => _type switch
    {
        BrushType.Solid => _solidColor.ToColorByte().ToHexCode(),
        BrushType.LinearGradient => "LinearGradient",
        _ => "?",
    };

    public static Brush Transparent => new Brush(Color4.Transparent);
    public static Brush White => new Brush(Color4.White);
    public static Brush Black => new Brush(Color4.Black);

    public BrushType Type => _type;

    static Brush() => RegistorSerdeConstructor();

    static partial void RegistorSerdeConstructor();

    private Brush(Color4 solidColor)
    {
        _type = BrushType.Solid;
        _solidColor = solidColor;
        _gradientStops = Array.Empty<GradientStop>();
    }

    private Brush(float directionRadian, GradientStop[] gradientStops)
    {
        if(gradientStops.Length < 2) {
            throw new ArgumentException("GradientStops must have at least 2 elements.");
        }
        // directionRadian must be in range [0, 2*PI)
        while(directionRadian < 0) {
            directionRadian += 2 * float.Pi;
        }
        directionRadian %= 2 * float.Pi;

        _type = BrushType.LinearGradient;
        _solidColor = default;
        _directionRadian = directionRadian;
        _gradientStops = gradientStops;
    }

    public bool TryGetSolidColor(out Color4 solidColor)
    {
        solidColor = _solidColor;
        return _type == BrushType.Solid;
    }

    public bool TryGetLinearGradient(out ImmutableArray<GradientStop> gradientStops)
    {
        if(_type != BrushType.LinearGradient) {
            gradientStops = default;
            return false;
        }
        gradientStops = ImmutableCollectionsMarshal.AsImmutableArray(_gradientStops);
        return true;
    }

    public Color4 SolidColor
    {
        get
        {
            if(_type != BrushType.Solid) {
                ThrowHelper.ThrowInvalidOperation($"{nameof(Type)} is not {nameof(BrushType.Solid)}");
            }
            return _solidColor;
        }
    }

    public ImmutableArray<GradientStop> GradientStops
    {
        get
        {
            if(_type != BrushType.LinearGradient) {
                ThrowHelper.ThrowInvalidOperation($"{nameof(Type)} is not {nameof(BrushType.LinearGradient)}");
            }
            return ImmutableCollectionsMarshal.AsImmutableArray(_gradientStops);
        }
    }

    public static Brush Solid(Color4 color)
    {
        return new Brush(color);
    }

    public static Brush LinearGradient(float directionRadian, ReadOnlySpan<GradientStop> gradientStops)
    {
        return new Brush(directionRadian, gradientStops.ToArray());
    }

    internal int GetBufferDataSize() => _type switch
    {
        BrushType.Solid => 48,
        BrushType.LinearGradient => 16 + _gradientStops.Length * 32,
        _ => throw new NotImplementedException(),
    };

    internal void GetBufferData<T>(T arg, ReadOnlySpanAction<byte, T> action)
    {
        GetBufferData((arg, action), static (span, x) =>
        {
            var (arg, action) = x;
            action.Invoke(span, arg);
            return (object?)null;
        });
    }

    internal TResult GetBufferData<T, TResult>(T arg, ReadOnlySpanFunc<byte, T, TResult> func)
    {
        // | position | size | type        | data       | note              |
        // | 0 - 3    | 4    | f32         | direction  | radian, [0, 2*Pi] |
        // | 4 - 7    | 4    | u32         | count      |                   |
        // | 8 - 15   | 8    |             | (padding)  |                   |
        // | 16 - 47  | 32   | Color4, f32 | ColorPoint |                   |
        // | 48 - 79  | 32   | Color4, f32 | ColorPoint |                   |
        // | ...      | 32   | Color4, f32 | ColorPoint |                   |

        switch(_type) {
            case BrushType.Solid: {
                Span<byte> span = stackalloc byte[16 + 32];
                // direction
                BinaryPrimitives.WriteSingleLittleEndian(span[0..4], 0f);
                // count
                BinaryPrimitives.WriteUInt32LittleEndian(span[4..8], 1u);
                var points = span[16..].MarshalCast<byte, UIShaderSource.ColorPoint>();
                points[0] = new()
                {
                    Color = _solidColor,
                    Offset = 0f,
                };
                return func.Invoke(span, arg);
            }
            case BrushType.LinearGradient: {
                using var mem = new ValueTypeRentMemory<byte>(16 + _gradientStops.Length * 32, false, out var span);

                var dir = _directionRadian;
                // make 'dir' into range of (0, 2 * Pi)
                dir %= (2 * MathF.PI);

                BinaryPrimitives.WriteSingleLittleEndian(span[0..4], dir);
                BinaryPrimitives.WriteUInt32LittleEndian(span[4..8], (uint)_gradientStops.Length);
                span[8..16].Clear();
                var points = span[16..].MarshalCast<byte, UIShaderSource.ColorPoint>();
                for(int i = 0; i < _gradientStops.Length; i++) {
                    points[i] = new()
                    {
                        Color = _gradientStops[i].Color,
                        Offset = _gradientStops[i].Offset,
                    };
                }
                return func.Invoke(span, arg);
            }
            default: {
                throw new NotImplementedException();
            }
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is Brush brush && Equals(brush);
    }

    public bool Equals(Brush other)
    {
        return _type == other._type &&
               _solidColor.Equals(other._solidColor) &&
               _directionRadian == other._directionRadian &&
               _gradientStops.AsSpan().SequenceEqual(other._gradientStops);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_type, _solidColor, _directionRadian, _gradientStops);
    }

    public static bool operator ==(Brush left, Brush right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Brush left, Brush right)
    {
        return !(left == right);
    }
}

public enum BrushType
{
    Solid = 0,
    LinearGradient,
    //RadialGradient,
}

public readonly record struct GradientStop(Color4 Color, float Offset);

internal static class LinearGradientParser
{
    [DebuggerDisplay("{DebugView}")]
    private struct ColorPosOrGradientCenter
    {
        private Color4 _color;
        private float? _colorPos1;
        private float _gradientCenter;
        private readonly bool _isColorPos;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebugView
        {
            get
            {
                if(_isColorPos) {
                    return _colorPos1 switch
                    {
                        null => $"{_color.ToColorByte().ToHexCode()}",
                        float pos1 => $"{_color.ToColorByte().ToHexCode()} {pos1 * 100:N0}%",
                    };
                }
                else {
                    return $"{_gradientCenter * 100:N0}%";
                }
            }
        }

        public Color4 Color
        {
            readonly get => _isColorPos ? _color : throw new InvalidOperationException();
            set
            {
                if(_isColorPos) {
                    _color = value;
                }
                else {
                    throw new InvalidOperationException();
                }
            }
        }
        public float? Pos
        {
            readonly get => _isColorPos ? _colorPos1 : throw new InvalidOperationException();
            set
            {
                if(_isColorPos) {
                    _colorPos1 = value;
                }
                else {
                    throw new InvalidOperationException();
                }
            }
        }
        public float GradientCenter
        {
            readonly get => _isColorPos ? throw new InvalidOperationException() : _gradientCenter;
            set
            {
                if(_isColorPos) {
                    throw new InvalidOperationException();
                }
                else {
                    _gradientCenter = value;
                }
            }
        }

        public ColorPosOrGradientCenter(Color4 color)
        {
            _color = color;
            _colorPos1 = null;
            _gradientCenter = 0;
            _isColorPos = true;
        }

        public ColorPosOrGradientCenter(Color4 color, float pos1)
        {
            _color = color;
            _colorPos1 = pos1;
            _gradientCenter = 0;
            _isColorPos = true;
        }

        public ColorPosOrGradientCenter(float gradientCenter)
        {
            _color = default;
            _colorPos1 = default;
            _gradientCenter = gradientCenter;
            _isColorPos = false;
        }

        public readonly bool IsColorPos(out Color4 color, out float? pos1)
        {
            if(_isColorPos) {
                color = _color;
                pos1 = _colorPos1;
                return true;
            }
            color = default;
            pos1 = default;
            return false;
        }
    }

    private static bool IsDegree(ReadOnlySpan<char> str, out float degree)
    {
        if(str.EndsWith("deg") && float.TryParse(str[..^3], out var deg)) {
            degree = deg;
            return true;
        }
        degree = 0;
        return false;
    }

    private static bool IsColor(ReadOnlySpan<char> str, out Color4 color)
    {
        return Color4.TryFromHexCode(str, out color) || Color4.TryFromWebColorName(str, out color);
    }

    private static bool IsPercent(ReadOnlySpan<char> str, out float percent)
    {
        if(str.EndsWith("%")) {
            return float.TryParse(str[..^1], out percent);
        }
        percent = 0;
        return false;
    }

    public static (float DirectionDegree, GradientStop[] Stops) ParseContent(ReadOnlySpan<char> str)
    {
        // "blue, red"
        // "blue 20%, red 80%"
        // "45deg, blue 20%, red 80%"
        // "45deg, blue 20%, 50%, red 80%"
        // "45deg, blue 20% 30%, 50%, red 80%"

        var degree = 0f;
        var first = true;
        var splits = str.Split(',', StringSplitOptions.TrimEntries);
        var list = new List<ColorPosOrGradientCenter>();
        foreach(var block in splits) {
            if(first && IsDegree(block, out var deg)) {
                degree = deg;
            }
            else {
                var blocks = block.Split(' ', StringSplitOptions.TrimEntries);
                var n = 0;
                ReadOnlySpan<char> block0;
                ReadOnlySpan<char> block1 = default;
                ReadOnlySpan<char> block2 = default;
                using(var e = blocks.GetEnumerator()) {
                    if(e.MoveNext() == false) { throw new FormatException(); }
                    block0 = e.Current;
                    n++;
                    if(e.MoveNext()) {
                        block1 = e.Current;
                        n++;
                        if(e.MoveNext()) {
                            block2 = e.Current;
                            n++;
                        }
                    }
                }
                if(n == 1) {
                    if(IsColor(block0, out var c)) {
                        list.Add(new ColorPosOrGradientCenter(c));
                    }
                    else if(!first && IsPercent(block0, out var p)) {
                        list.Add(new ColorPosOrGradientCenter(p * 0.01f));
                    }
                    else {
                        throw new FormatException();
                    }
                }
                else if(n == 2) {
                    if(IsColor(block0, out var color) == false) {
                        throw new FormatException();
                    }
                    if(IsPercent(block1, out var p1) == false) {
                        throw new FormatException();
                    }
                    list.Add(new ColorPosOrGradientCenter(color, p1 * 0.01f));
                }
                else {
                    if(IsColor(block0, out var color) == false) {
                        throw new FormatException();
                    }
                    if(IsPercent(block1, out var p1) == false) {
                        throw new FormatException();
                    }
                    if(IsPercent(block2, out var p2) == false) {
                        throw new FormatException();
                    }
                    list.Add(new ColorPosOrGradientCenter(color, p1 * 0.01f));
                    list.Add(new ColorPosOrGradientCenter(color, p2 * 0.01f));
                }
            }
            first = false;
        }

        var data = list.AsSpan();
        Validate(data);
        var stops = new GradientStop[data.Length];
        for(int i = 0; i < stops.Length; i++) {
            var offset = data[i].Pos;
            Debug.Assert(offset.HasValue);
            stops[i] = new GradientStop(data[i].Color, offset.Value);
        }
        return (degree, stops);
    }

    private static void Validate(Span<ColorPosOrGradientCenter> span)
    {
        if(span.Length < 2) {
            throw new FormatException();
        }

        {
            if(span[0].IsColorPos(out var color, out var p) == false) {
                throw new FormatException();
            }
            span[0].Pos = p ?? 0;
        }
        {
            if(span[^1].IsColorPos(out var color, out var p) == false) {
                throw new FormatException();
            }
            span[^1].Pos = p ?? 1;
        }

        float currentPos = span[0].Pos!.Value;
        for(int i = 0; i < span.Length; i++) {
            if(span[i].IsColorPos(out var color, out float? p)) {
                float p_;
                if(p == null) {
                    var q = span[(i + 1)..].First(
                        out var index,
                        static x => x.IsColorPos(out _, out var p) && p.HasValue).Pos!.Value;
                    p_ = currentPos + (q - currentPos) / (float)(index + 2);
                }
                else {
                    p_ = float.Max(p.Value, currentPos);
                }
                span[i].Pos = p_;
                currentPos = p_;
            }
            else {
                span[i].GradientCenter = float.Max(currentPos, span[i].GradientCenter);
            }
        }

        for(int i = 0; i < span.Length; i++) {
            if(span[i].IsColorPos(out _, out _)) { continue; }
            var center = span[i].GradientCenter;
            if(span[i - 1].IsColorPos(out var prevColor, out var prev) == false) { throw new FormatException(); }
            if(span[i + 1].IsColorPos(out var nextColor, out var next) == false) { throw new FormatException(); }
            Debug.Assert(prev.HasValue);
            Debug.Assert(next.HasValue);
            var diff1 = center - prev.Value;
            var diff2 = next.Value - center;
            if(diff1 < diff2) {
                span[i] = new ColorPosOrGradientCenter(nextColor, prev.Value + diff1 * 2);
            }
            else {
                span[i] = new ColorPosOrGradientCenter(prevColor, next.Value - diff2 * 2);
            }
        }
    }
}

file static class SpanExtensions
{
    public static ref T First<T>(this Span<T> span, out int index, Func<T, bool> condition)
    {
        for(int i = 0; i < span.Length; i++) {
            if(condition(span[i])) {
                index = i;
                return ref span[i];
            }
        }

        throw new InvalidOperationException();
    }
}
