#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Hikari.UI;

public readonly struct BoxShadow
    : IEquatable<BoxShadow>,
      IFromJson<BoxShadow>,
      IToJson
{
    private readonly float _offsetX;
    private readonly float _offsetY;
    private readonly float _blurRadius;
    private readonly float _spreadRadius;
    private readonly Color4 _color;
    private readonly bool _isInset;

    public float OffsetX
    {
        get => _offsetX;
        init
        {
            _offsetX = value;
        }
    }
    public float OffsetY
    {
        get => _offsetY;
        init
        {
            _offsetY = value;
        }
    }
    public float BlurRadius
    {
        get => _blurRadius;
        init
        {
            if(value < 0) {
                ThrowArgOutOfRange(nameof(value));
            }
            _blurRadius = value;
        }
    }
    public float SpreadRadius
    {
        get => _spreadRadius;
        init
        {
            _spreadRadius = value;
        }
    }
    public Color4 Color
    {
        get => _color;
        init
        {
            _color = value;
        }
    }
    public bool IsInset
    {
        get => _isInset;
        init
        {
            _isInset = value;
        }
    }

    [DoesNotReturn]
    private static void ThrowArgOutOfRange(string paramName) => throw new ArgumentOutOfRangeException(paramName);

    public static BoxShadow None => default;

    static BoxShadow() => Serializer.RegisterConstructor(FromJson);

    public BoxShadow()
    {
    }

    public override bool Equals(object? obj)
    {
        return obj is BoxShadow shadow && Equals(shadow);
    }

    public bool Equals(BoxShadow other)
    {
        return OffsetX == other.OffsetX &&
               OffsetY == other.OffsetY &&
               BlurRadius == other.BlurRadius &&
               SpreadRadius == other.SpreadRadius &&
               Color.Equals(other.Color) &&
               IsInset == other.IsInset;
    }

    public override int GetHashCode() => HashCode.Combine(OffsetX, OffsetY, BlurRadius, SpreadRadius, Color, IsInset);

    public static BoxShadow FromJson(in ObjectSource source)
    {
        switch(source.ValueKind) {
            case JsonValueKind.String: {
                // none
                // OffsetX OffsetY Color
                // OffsetX OffsetY BlurRadius Color
                // OffsetX OffsetY BlurRadius SpreadRadius Color
                // inset OffsetX OffsetY Color
                // inset OffsetX OffsetY BlurRadius Color
                // inset OffsetX OffsetY BlurRadius SpreadRadius Color

                var str = source.GetStringNotNull();
                if(str == "none") {
                    return None;
                }

                var splits = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                ReadOnlySpan<string> values;
                if(TryParseInset(splits[0], out var inset)) {
                    values = splits[1..];
                }
                else {
                    values = splits;
                    inset = false;
                }
                return values.Length switch
                {
                    3 => new BoxShadow
                    {
                        IsInset = inset,
                        OffsetX = ParseLength(values[0]),
                        OffsetY = ParseLength(values[1]),
                        Color = ParseColor(values[2]),
                    },
                    4 => new BoxShadow
                    {
                        IsInset = inset,
                        OffsetX = ParseLength(values[0]),
                        OffsetY = ParseLength(values[1]),
                        BlurRadius = ParseLength(values[2]),
                        Color = ParseColor(values[3]),
                    },
                    5 => new BoxShadow
                    {
                        IsInset = inset,
                        OffsetX = ParseLength(values[0]),
                        OffsetY = ParseLength(values[1]),
                        BlurRadius = ParseLength(values[2]),
                        SpreadRadius = ParseLength(values[3]),
                        Color = ParseColor(values[4]),
                    },
                    _ => throw new FormatException("Invalid BoxShadow format"),
                };
            }
            default: {
                source.ThrowInvalidFormat();
                return default;
            }
        }

        static bool TryParseInset(ReadOnlySpan<char> split, out bool inset)
        {
            if(split.SequenceEqual("inset")) {
                inset = true;
                return true;
            }
            inset = false;
            return false;
        }

        static float ParseLength(ReadOnlySpan<char> str)
        {
            if(str.EndsWith("px")) {
                return float.Parse(str[..^2]);
            }
            else {
                throw new FormatException("invalid length format");
            }
        }

        static Color4 ParseColor(ReadOnlySpan<char> str)
        {
            if(Color4.TryFromHexCode(str, out var color) || Color4.TryFromWebColorName(str, out color)) {
                return color;
            }
            else {
                throw new FormatException("invalid color format");
            }
        }
    }

    public JsonValueKind ToJson(Utf8JsonWriter writer)
    {
        if(this == None) {
            writer.WriteStringValue("none");
            return JsonValueKind.String;
        }
        else {
            if(IsInset) {
                writer.WriteStringValue($"inset {OffsetX}px {OffsetY}px {BlurRadius}px {SpreadRadius}px {Color.ToColorByte().ToHexCode()}");

            }
            else {
                writer.WriteStringValue($"{OffsetX}px {OffsetY}px {BlurRadius}px {SpreadRadius}px {Color.ToColorByte().ToHexCode()}");
            }
        }
        return JsonValueKind.String;
    }

    public static bool operator ==(BoxShadow left, BoxShadow right) => left.Equals(right);

    public static bool operator !=(BoxShadow left, BoxShadow right) => !(left == right);
}
