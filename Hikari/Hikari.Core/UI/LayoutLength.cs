#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Hikari.UI;

[DebuggerDisplay("{DebugView}")]
public readonly partial struct LayoutLength : IEquatable<LayoutLength>
{
    public required float Value { get; init; }
    public required LayoutLengthType Type { get; init; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebugView => Type switch
    {
        LayoutLengthType.Length => $"{Value}px",
        LayoutLengthType.Proportion => $"{Value * 100f}%",
        _ => "?",
    };

    static LayoutLength() => RegistorSerdeConstructor();

    static partial void RegistorSerdeConstructor();

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

    public static LayoutLength Length(float length) => new LayoutLength(length, LayoutLengthType.Length);

    public static LayoutLength Proportion(float proportion) => new LayoutLength(proportion, LayoutLengthType.Proportion);

    public override bool Equals(object? obj) => obj is LayoutLength length && Equals(length);

    public bool Equals(LayoutLength other) => Value == other.Value && Type == other.Type;

    public override int GetHashCode() => HashCode.Combine(Value, Type);

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

public enum TextAlignment
{
    Center = 0,
    Left,
    Right,
}
