#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Elffy.UI;

public readonly struct CornerRadius
    : IEquatable<CornerRadius>,
      IFromJson<CornerRadius>,
      IToJson
{
    public required float TopLeft { get; init; }
    public required float TopRight { get; init; }
    public required float BottomRight { get; init; }
    public required float BottomLeft { get; init; }

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

    public static CornerRadius FromJson(JsonElement element)
    {
        switch(element.ValueKind) {
            case JsonValueKind.Number: {
                return new CornerRadius(element.GetSingle());
            }
            case JsonValueKind.String: {
                var str = element.GetStringNotNull();
                var splits = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return splits switch
                {
                    { Length: 1 } => new CornerRadius(float.Parse(splits[0])),
                    { Length: 2 } => new CornerRadius(float.Parse(splits[0]), float.Parse(splits[1])),
                    { Length: 3 } => new CornerRadius(float.Parse(splits[0]), float.Parse(splits[1]), float.Parse(splits[2])),
                    { Length: 4 } => new CornerRadius(float.Parse(splits[0]), float.Parse(splits[1]), float.Parse(splits[2]), float.Parse(splits[3])),
                    _ => throw new FormatException($"cannot create {nameof(Thickness)} from string \"{str}\""),
                };
            }
            default: {
                throw new FormatException(element.ToString());
            }
        }
    }

    public JsonNode? ToJson()
    {
        return $"{TopLeft} {TopRight} {BottomRight} {BottomLeft}";
    }

    public Vector4 ToVector4() => new Vector4(TopLeft, TopRight, BottomRight, BottomLeft);

    public static bool operator ==(CornerRadius left, CornerRadius right) => left.Equals(right);

    public static bool operator !=(CornerRadius left, CornerRadius right) => !(left == right);
}
