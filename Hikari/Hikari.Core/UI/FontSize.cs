#nullable enable
using System;

namespace Hikari.UI;

public readonly partial struct FontSize : IEquatable<FontSize>
{
    private readonly float _px;
    public float Px => _px;

    static FontSize() => RegistorSerdeConstructor();

    static partial void RegistorSerdeConstructor();

    public FontSize(float px)
    {
        _px = px;
    }

    public static implicit operator FontSize(float value) => new(value);

    public static implicit operator FontSize(int value) => new(value);

    public static bool operator ==(FontSize left, FontSize right) => left.Equals(right);

    public static bool operator !=(FontSize left, FontSize right) => !(left == right);

    public override bool Equals(object? obj) => obj is FontSize size && Equals(size);

    public bool Equals(FontSize other) => _px == other._px;

    public override int GetHashCode() => HashCode.Combine(_px);
}
