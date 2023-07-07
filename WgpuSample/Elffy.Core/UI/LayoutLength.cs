#nullable enable
using System;
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

    public static LayoutLength FromJson(JsonElement element)
    {
        // 10
        // "10"
        // "0.8*"

        switch(element.ValueKind) {
            case JsonValueKind.Number: {
                return new LayoutLength(element.GetSingle(), LayoutLengthType.Length);
            }
            case JsonValueKind.String: {
                var str = element.GetStringNotNull();
                return
                    str.EndsWith('*') ?
                    new()
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
            default: {
                throw new FormatException(element.ToString());
            }
        }
    }

    public JsonNode? ToJson()
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
