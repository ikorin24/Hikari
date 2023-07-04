#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.UI;

public readonly struct LayoutLength : IEquatable<LayoutLength>
{
    public readonly float Value;
    public readonly LayoutLengthType Type;

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

    public override string ToString()
    {
        return Type switch
        {
            LayoutLengthType.Length => Value.ToString("px"),
            LayoutLengthType.Proportion => Value.ToString("0.00%"),
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
    Length,
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
