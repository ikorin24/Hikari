#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace Hikari.UI;

public readonly partial struct BoxShadow : IEquatable<BoxShadow>
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

    static BoxShadow() => RegistorSerdeConstructor();

    static partial void RegistorSerdeConstructor();

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

    public static bool operator ==(BoxShadow left, BoxShadow right) => left.Equals(right);

    public static bool operator !=(BoxShadow left, BoxShadow right) => !(left == right);
}
