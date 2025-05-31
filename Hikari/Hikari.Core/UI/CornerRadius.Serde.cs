#if HIKARI_JSON_SERDE
#nullable enable
using System;
using System.Text.Json;

namespace Hikari.UI;

partial struct CornerRadius : IFromJson<CornerRadius>, IToJson
{
    static partial void RegistorSerdeConstructor()
    {
        Serializer.RegisterConstructor(FromJson);
    }

    public static CornerRadius FromJson(in ObjectSource source)
    {
        switch(source.ValueKind) {
            case JsonValueKind.Number: {
                return new CornerRadius(source.GetNumber<float>());
            }
            case JsonValueKind.String: {
                var str = source.GetStringNotNull();
                var splits = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return splits switch
                {
                    { Length: 1 } => new CornerRadius(ParseValue(splits[0])),
                    { Length: 2 } => new CornerRadius(ParseValue(splits[0]), ParseValue(splits[1])),
                    { Length: 3 } => new CornerRadius(ParseValue(splits[0]), ParseValue(splits[1]), ParseValue(splits[2])),
                    { Length: 4 } => new CornerRadius(ParseValue(splits[0]), ParseValue(splits[1]), ParseValue(splits[2]), ParseValue(splits[3])),
                    _ => throw new FormatException($"cannot create {nameof(CornerRadius)} from string \"{str}\""),
                };
            }
            default: {
                source.ThrowInvalidFormat();
                return default;
            }
        }

        static float ParseValue(ReadOnlySpan<char> x)
        {
            if(x.EndsWith("px")) {
                return float.Parse(x[..^2]);
            }
            throw new FormatException();
        }
    }

    public JsonValueKind ToJson(Utf8JsonWriter writer)
    {
        writer.WriteStringValue($"{TopLeft}px {TopRight}px {BottomRight}px {BottomLeft}px");
        return JsonValueKind.String;
    }
}
#endif
