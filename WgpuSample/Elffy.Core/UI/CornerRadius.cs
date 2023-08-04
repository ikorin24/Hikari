#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Elffy.UI;

[DebuggerDisplay("{DebugView}")]
public readonly struct CornerRadius
    : IEquatable<CornerRadius>,
      IFromJson<CornerRadius>,
      IToJson
{
    public required float TopLeft { get; init; }
    public required float TopRight { get; init; }
    public required float BottomRight { get; init; }
    public required float BottomLeft { get; init; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebugView => $"{TopLeft}px {TopRight}px {BottomRight}px {BottomLeft}px";

    public static CornerRadius Zero => default;

    static CornerRadius() => Serializer.RegisterConstructor(FromJson);

    [SetsRequiredMembers]
    public CornerRadius(float value)
    {
        TopLeft = value;
        TopRight = value;
        BottomRight = value;
        BottomLeft = value;
    }

    [SetsRequiredMembers]
    public CornerRadius(float topLeftAndBottomRight, float topRightAndBottomLeft)
    {
        TopLeft = topLeftAndBottomRight;
        TopRight = topRightAndBottomLeft;
        BottomRight = topLeftAndBottomRight;
        BottomLeft = topRightAndBottomLeft;
    }

    [SetsRequiredMembers]
    public CornerRadius(float topLeft, float topRightAndBottomLeft, float bottomRight)
    {
        TopLeft = topLeft;
        TopRight = topRightAndBottomLeft;
        BottomRight = bottomRight;
        BottomLeft = topRightAndBottomLeft;
    }

    [SetsRequiredMembers]
    public CornerRadius(float topLeft, float topRight, float bottomRight, float bottomLeft)
    {
        TopLeft = topLeft;
        TopRight = topRight;
        BottomRight = bottomRight;
        BottomLeft = bottomLeft;
    }

    public override bool Equals(object? obj)
    {
        return obj is CornerRadius radius && Equals(radius);
    }

    public bool Equals(CornerRadius other)
    {
        return TopLeft == other.TopLeft &&
               TopRight == other.TopRight &&
               BottomRight == other.BottomRight &&
               BottomLeft == other.BottomLeft;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TopLeft, TopRight, BottomRight, BottomLeft);
    }

    public static CornerRadius FromJson(in ReactSource source)
    {
        switch(source.ValueKind) {
            case JsonValueKind.Number: {
                return new CornerRadius(source.GetNumber<float>());
            }
            case JsonValueKind.String: {
                var str = source.GetStringNotNull();
                var splits = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return splits switch
                {
                    { Length: 1 } => new CornerRadius(ParseValue(splits[0])),
                    { Length: 2 } => new CornerRadius(ParseValue(splits[0]), ParseValue(splits[1])),
                    { Length: 3 } => new CornerRadius(ParseValue(splits[0]), ParseValue(splits[1]), ParseValue(splits[2])),
                    { Length: 4 } => new CornerRadius(ParseValue(splits[0]), ParseValue(splits[1]), ParseValue(splits[2]), ParseValue(splits[3])),
                    _ => throw new FormatException($"cannot create {nameof(Thickness)} from string \"{str}\""),
                };
            }
            default: {
                source.ThrowInvalidFormat();
                return default;
            }
        }

        static float ParseValue(ReadOnlySpan<char> x)
        {
            if(x.EndsWith("px")) {
                return float.Parse(x[..^2]);
            }
            throw new FormatException();
        }
    }

    public JsonValueKind ToJson(Utf8JsonWriter writer)
    {
        writer.WriteStringValue($"{TopLeft}px {TopRight}px {BottomRight}px {BottomLeft}px");
        return JsonValueKind.String;
    }

    public Vector4 ToVector4() => new Vector4(TopLeft, TopRight, BottomRight, BottomLeft);

    public static bool operator ==(CornerRadius left, CornerRadius right) => left.Equals(right);

    public static bool operator !=(CornerRadius left, CornerRadius right) => !(left == right);
}
