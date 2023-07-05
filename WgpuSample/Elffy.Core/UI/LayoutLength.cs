#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Elffy.UI;

public readonly struct LayoutLength
    : IEquatable<LayoutLength>,
      IFromJson<LayoutLength>,
      IToJson
{
    public required float Value { get; init; }
    public required LayoutLengthType Type { get; init; }

    static LayoutLength() => Serializer.RegisterConstructor(FromJson);

    [SetsRequiredMembers]
    public LayoutLength(float value, LayoutLengthType type)
    {
        if(value < 0) {
            ThrowOutOfRange();
        }
        Value = value;
        Type = type;

        [DoesNotReturn] static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(value));
    }

    public static LayoutLength FromJson(JsonNode? node)
    {
        switch(node) {
            case JsonValue value: {
                // [value pattern]
                // 10
                // "10"
                // "0.8*"

                if(value.TryGetValue<float>(out var number)) {
                    return new LayoutLength(number, LayoutLengthType.Length);
                }
                else if(value.TryGetValue<string>(out var str)) {
                    return
                        str.EndsWith("*") ? new()
                        {
                            Value = float.Parse(str.AsSpan(0, str.Length - 1)),
                            Type = LayoutLengthType.Proportion,
                        } :
                        new()
                        {
                            Value = float.Parse(str),
                            Type = LayoutLengthType.Length,
                        };
                }
                else {
                    throw new FormatException("value should be number or string");
                }
            }
            default: {
                throw new FormatException("invalid format");
            }
        }
    }

    public JsonNode? ToJson(JsonSerializerOptions? options = null)
    {
        return Type switch
        {
            LayoutLengthType.Length => Value,
            LayoutLengthType.Proportion => $"{Value}*",
            _ => 0,
        };
    }

    public static LayoutLength Length(float length) => new LayoutLength(length, LayoutLengthType.Length);

    public static LayoutLength Proportion(float proportion) => new LayoutLength(proportion, LayoutLengthType.Proportion);

    public override bool Equals(object? obj) => obj is LayoutLength length && Equals(length);

    public bool Equals(LayoutLength other) => Value == other.Value && Type == other.Type;

    public override int GetHashCode() => HashCode.Combine(Value, Type);

    public override string ToString()
    {
        return Type switch
        {
            LayoutLengthType.Length => Value.ToString(),
            LayoutLengthType.Proportion => $"{Value}*",
            _ => base.ToString() ?? "",
        };
    }

    public static bool operator ==(LayoutLength left, LayoutLength right) => left.Equals(right);

    public static bool operator !=(LayoutLength left, LayoutLength right) => !(left == right);

    public static implicit operator LayoutLength(float value) => new LayoutLength(value, LayoutLengthType.Length);
    public static implicit operator LayoutLength(int value) => new LayoutLength(value, LayoutLengthType.Length);
}

public enum LayoutLengthType
{
    Length = 0,
    Proportion,
}

public enum HorizontalAlignment
{
    Center = 0,
    Left,
    Right,
}

public enum VerticalAlignment
{
    Center = 0,
    Top,
    Bottom,
}

[DebuggerDisplay("{DebugDisplay}")]
public struct LayoutThickness : IEquatable<LayoutThickness>
{
    public float Left;
    public float Top;
    public float Right;
    public float Bottom;

    public static LayoutThickness Zero => default;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebugDisplay => $"({Left}, {Top}, {Right}, {Bottom})";

    public LayoutThickness(float value)
    {
        Left = value;
        Top = value;
        Right = value;
        Bottom = value;
    }

    public LayoutThickness(float left, float top, float right, float bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public override string ToString() => DebugDisplay;

    public override bool Equals(object? obj) => obj is LayoutThickness thickness && Equals(thickness);

    public bool Equals(LayoutThickness other) => Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;

    public override int GetHashCode() => HashCode.Combine(Left, Top, Right, Bottom);

    public static bool operator ==(LayoutThickness left, LayoutThickness right) => left.Equals(right);

    public static bool operator !=(LayoutThickness left, LayoutThickness right) => !(left == right);
}
