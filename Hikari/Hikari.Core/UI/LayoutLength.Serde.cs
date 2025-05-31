#if HIKARI_JSON_SERDE
#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Hikari.UI;

partial struct LayoutLength : IFromJson<LayoutLength>, IToJson
{
    static partial void RegistorSerdeConstructor()
    {
        Serializer.RegisterConstructor(FromJson);
    }
    public static LayoutLength FromJson(in ObjectSource source)
    {
        // 10
        // "10px"
        // "80%"

        switch(source.ValueKind) {
            case JsonValueKind.Number: {
                return new LayoutLength(source.GetNumber<float>(), LayoutLengthType.Length);
            }
            case JsonValueKind.String: {
                var str = source.GetStringNotNull();
                if(str.EndsWith('%')) {
                    return new()
                    {
                        Value = float.Parse(str.AsSpan()[..^1]) * 0.01f,
                        Type = LayoutLengthType.Proportion,
                    };
                }
                if(str.EndsWith("px")) {
                    return new()
                    {
                        Value = float.Parse(str.AsSpan()[..^2]),
                        Type = LayoutLengthType.Length,
                    };
                }
                throw new FormatException(str);
            }
            default: {
                source.ThrowInvalidFormat();
                return default;
            }
        }
    }

    public JsonValueKind ToJson(Utf8JsonWriter writer)
    {
        switch(Type) {
            case LayoutLengthType.Length: {
                writer.WriteNumberValue(Value);
                return JsonValueKind.Number;
            }
            case LayoutLengthType.Proportion: {
                writer.WriteStringValue($"{Value * 100f}%");
                return JsonValueKind.String;
            }
            default: {
                throw new UnreachableException();
            }
        }
    }
}
#endif
