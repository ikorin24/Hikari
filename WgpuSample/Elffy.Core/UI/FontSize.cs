#nullable enable
using Elffy.Effective;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Elffy.UI;

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

    public static FontSize FromJson(JsonElement element, in DeserializeRuntimeData data)
    {
        switch(element.ValueKind) {
            case JsonValueKind.String: {
                var str = element.GetStringNotNull();
                if(str.EndsWith("px")) {
                    return new FontSize(float.Parse(str.AsSpan()[..^2]));
                }
                throw new FormatException(str);
            }
            case JsonValueKind.Number: {
                return new FontSize(element.GetSingle());
            }
            default: {
                throw new FormatException(element.ToString());
            }
        }
    }

    public JsonNode? ToJson()
    {
        return $"{_px}px";
    }

    public static implicit operator FontSize(float value) => new(value);

    public static implicit operator FontSize(int value) => new(value);

    public static bool operator ==(FontSize left, FontSize right) => left.Equals(right);

    public static bool operator !=(FontSize left, FontSize right) => !(left == right);

    public override bool Equals(object? obj) => obj is FontSize size && Equals(size);

    public bool Equals(FontSize other) => _px == other._px;

    public override int GetHashCode() => HashCode.Combine(_px);
}
