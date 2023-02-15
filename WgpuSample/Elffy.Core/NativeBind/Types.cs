#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.NativeBind;

/// <summary>
/// `Option&lt;NonZeroU32&gt;` in Rust
/// </summary>
internal readonly struct OptionNonZeroU32 : IEquatable<OptionNonZeroU32>
{
    private readonly u32 _value;
    public static OptionNonZeroU32 None => default;

    private OptionNonZeroU32(u32 value) => _value = value;

    public static implicit operator OptionNonZeroU32(u32 value) => new(value);

    public static bool operator ==(OptionNonZeroU32 left, OptionNonZeroU32 right) => left.Equals(right);

    public static bool operator !=(OptionNonZeroU32 left, OptionNonZeroU32 right) => !(left == right);

    public override bool Equals(object? obj) => obj is OptionNonZeroU32 none && Equals(none);

    public bool Equals(OptionNonZeroU32 other) => _value == other._value;

    public override int GetHashCode() => _value.GetHashCode();
}
