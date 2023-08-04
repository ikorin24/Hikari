#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Elffy.UI;

[DebuggerDisplay("{DebugDisplay}")]
public readonly struct Thickness
    : IEquatable<Thickness>,
      IFromJson<Thickness>,
      IToJson
{
    public required float Top { get; init; }
    public required float Right { get; init; }
    public required float Bottom { get; init; }
    public required float Left { get; init; }

    public static Thickness Zero => default;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebugDisplay => $"{Top}px {Right}px {Bottom}px {Left}px";

    static Thickness() => Serializer.RegisterConstructor(FromJson);

    [SetsRequiredMembers]
    public Thickness(float value)
    {
        Top = value;
        Right = value;
        Bottom = value;
        Left = value;
    }

    [SetsRequiredMembers]
    public Thickness(float topBottom, float leftRight)
    {
        Top = topBottom;
        Right = leftRight;
        Bottom = topBottom;
        Left = leftRight;
    }

    [SetsRequiredMembers]
    public Thickness(float top, float leftRight, float bottom)
    {
        Top = top;
        Right = leftRight;
        Bottom = bottom;
        Left = leftRight;
    }

    [SetsRequiredMembers]
    public Thickness(float top, float right, float bottom, float left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    public override string ToString() => DebugDisplay;

    public override bool Equals(object? obj) => obj is Thickness thickness && Equals(thickness);

    public bool Equals(Thickness other) => Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;

    public override int GetHashCode() => HashCode.Combine(Left, Top, Right, Bottom);

    public static Thickness FromJson(in ReactSource source)
    {
        // 10
        // "10px"
        // "10px"
        // "10px 10px 10px"
        // "10px 10px 10px 10px"

        switch(source.ValueKind) {
            case JsonValueKind.Number: {
                return new Thickness(source.GetNumber<float>());
            }
            case JsonValueKind.String: {
                var str = source.GetStringNotNull();
                var splits = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return splits switch
                {
                    { Length: 1 } => new Thickness(Px(splits[0])),
                    { Length: 2 } => new Thickness(Px(splits[0]), Px(splits[1])),
                    { Length: 3 } => new Thickness(Px(splits[0]), Px(splits[1]), Px(splits[2])),
                    { Length: 4 } => new Thickness(Px(splits[0]), Px(splits[1]), Px(splits[2]), Px(splits[3])),
                    _ => throw new FormatException($"cannot create {nameof(Thickness)} from string \"{str}\""),
                };
            }
            default: {
                source.ThrowInvalidFormat();
                return default;
            }
        }

        static float Px(ReadOnlySpan<char> s)
        {
            if(s.EndsWith("px")) {
                return float.Parse(s[..^2]);
            }
            throw new FormatException();
        }
    }

    public JsonValueKind ToJson(Utf8JsonWriter writer)
    {
        writer.WriteStringValue($"{Top}px {Right}px {Bottom}px {Left}px");
        return JsonValueKind.String;
    }

    public static bool operator ==(Thickness left, Thickness right) => left.Equals(right);

    public static bool operator !=(Thickness left, Thickness right) => !(left == right);

    public Vector4 ToVector4() => new Vector4(Top, Right, Bottom, Left);
}
