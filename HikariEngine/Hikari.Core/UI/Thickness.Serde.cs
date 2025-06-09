#if HIKARI_JSON_SERDE
#nullable enable
using System;
using System.Text.Json;

namespace Hikari.UI;

partial struct Thickness : IFromJson<Thickness>, IToJson
{
    static partial void RegistorSerdeConstructor()
    {
        Serializer.RegisterConstructor(FromJson);
    }

    public static Thickness FromJson(in ObjectSource source)
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
}
#endif
