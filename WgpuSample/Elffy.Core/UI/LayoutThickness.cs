#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Elffy.UI;

[DebuggerDisplay("{DebugDisplay}")]
public readonly struct LayoutThickness
    : IEquatable<LayoutThickness>,
      IFromJson<LayoutThickness>,
      IToJson
{
    public required float Top { get; init; }
    public required float Right { get; init; }
    public required float Bottom { get; init; }
    public required float Left { get; init; }

    public static LayoutThickness Zero => default;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebugDisplay => $"{Top} {Right} {Bottom} {Left}";

    static LayoutThickness() => Serializer.RegisterConstructor(FromJson);

    [SetsRequiredMembers]
    public LayoutThickness(float value)
    {
        Top = value;
        Right = value;
        Bottom = value;
        Left = value;
    }

    [SetsRequiredMembers]
    public LayoutThickness(float topBottom, float leftRight)
    {
        Top = topBottom;
        Right = leftRight;
        Bottom = topBottom;
        Left = leftRight;
    }

    [SetsRequiredMembers]
    public LayoutThickness(float top, float leftRight, float bottom)
    {
        Top = top;
        Right = leftRight;
        Bottom = bottom;
        Left = leftRight;
    }

    [SetsRequiredMembers]
    public LayoutThickness(float top, float right, float bottom, float left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    public override string ToString() => DebugDisplay;

    public override bool Equals(object? obj) => obj is LayoutThickness thickness && Equals(thickness);

    public bool Equals(LayoutThickness other) => Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;

    public override int GetHashCode() => HashCode.Combine(Left, Top, Right, Bottom);

    public static LayoutThickness FromJson(JsonElement element)
    {
        // 10.3
        // "10.3"
        // "10.3"
        // "10.3 10.3 10.3"
        // "10.3 10.3 10.3 10.3"

        switch(element.ValueKind) {
            case JsonValueKind.Number: {
                return new LayoutThickness(element.GetSingle());
            }
            case JsonValueKind.String: {
                var str = element.GetStringNotNull();
                var splits = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return splits switch
                {
                    { Length: 1 } => new LayoutThickness(float.Parse(splits[0])),
                    { Length: 2 } => new LayoutThickness(float.Parse(splits[0]), float.Parse(splits[1])),
                    { Length: 3 } => new LayoutThickness(float.Parse(splits[0]), float.Parse(splits[1]), float.Parse(splits[2])),
                    { Length: 4 } => new LayoutThickness(float.Parse(splits[0]), float.Parse(splits[1]), float.Parse(splits[2]), float.Parse(splits[3])),
                    _ => throw new FormatException($"cannot create {nameof(LayoutThickness)} from string \"{str}\""),
                };
            }
            default: {
                throw new FormatException(element.ToString());
            }
        }
    }

    public JsonNode? ToJson()
    {
        return $"{Top} {Right} {Bottom} {Left}";
    }

    public static bool operator ==(LayoutThickness left, LayoutThickness right) => left.Equals(right);

    public static bool operator !=(LayoutThickness left, LayoutThickness right) => !(left == right);
}
