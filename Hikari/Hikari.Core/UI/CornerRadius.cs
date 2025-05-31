#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Hikari.UI;

[DebuggerDisplay("{DebugView}")]
public readonly partial struct CornerRadius : IEquatable<CornerRadius>
{
    public required float TopLeft { get; init; }
    public required float TopRight { get; init; }
    public required float BottomRight { get; init; }
    public required float BottomLeft { get; init; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebugView => $"{TopLeft}px {TopRight}px {BottomRight}px {BottomLeft}px";

    public static CornerRadius Zero => default;

    static CornerRadius() => RegistorSerdeConstructor();

    static partial void RegistorSerdeConstructor();

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

    public Vector4 ToVector4() => new Vector4(TopLeft, TopRight, BottomRight, BottomLeft);

    public static bool operator ==(CornerRadius left, CornerRadius right) => left.Equals(right);

    public static bool operator !=(CornerRadius left, CornerRadius right) => !(left == right);
}
