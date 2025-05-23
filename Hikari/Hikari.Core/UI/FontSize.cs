﻿#nullable enable
using System;
using System.Text.Json;

namespace Hikari.UI;

public readonly struct FontSize
    : IEquatable<FontSize>,
      IFromJson<FontSize>,
      IToJson
{
    private readonly float _px;
    public float Px => _px;

    static FontSize() => Serializer.RegisterConstructor(FromJson);

    public FontSize(float px)
    {
        _px = px;
    }

    public static FontSize FromJson(in ObjectSource source)
    {
        switch(source.ValueKind) {
            case JsonValueKind.String: {
                var str = source.GetStringNotNull();
                if(str.EndsWith("px")) {
                    return new FontSize(float.Parse(str.AsSpan()[..^2]));
                }
                throw new FormatException(str);
            }
            case JsonValueKind.Number: {
                return new FontSize(source.GetNumber<float>());
            }
            default: {
                source.ThrowInvalidFormat();
                return default;
            }
        }
    }

    public JsonValueKind ToJson(Utf8JsonWriter writer)
    {
        writer.WriteStringValue($"{_px}px");
        return JsonValueKind.String;
    }

    public static implicit operator FontSize(float value) => new(value);

    public static implicit operator FontSize(int value) => new(value);

    public static bool operator ==(FontSize left, FontSize right) => left.Equals(right);

    public static bool operator !=(FontSize left, FontSize right) => !(left == right);

    public override bool Equals(object? obj) => obj is FontSize size && Equals(size);

    public bool Equals(FontSize other) => _px == other._px;

    public override int GetHashCode() => HashCode.Combine(_px);
}
