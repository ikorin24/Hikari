#if HIKARI_JSON_SERDE
#nullable enable
using System;
using System.Text.Json;

namespace Hikari.UI;

partial struct FontSize : IFromJson<FontSize>, IToJson
{
    static partial void RegistorSerdeConstructor()
    {
        Serializer.RegisterConstructor(FromJson);
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
}
#endif
