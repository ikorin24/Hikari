#if HIKARI_JSON_SERDE
#nullable enable
using Hikari;
using Hikari.Mathematics;
using System;
using System.Diagnostics;
using System.Text.Json;

namespace Hikari.UI;

partial struct Brush : IFromJson<Brush>, IToJson
{
    static partial void RegistorSerdeConstructor()
    {
        Serializer.RegisterConstructor(FromJson);
    }

    public static Brush FromJson(in ObjectSource source)
    {
        // "#ffee23"
        // "red"

        switch(source.ValueKind) {
            case JsonValueKind.String: {
                var str = source.GetStringNotNull();
                if(str.StartsWith("LinearGradient(") && str.EndsWith(")")) {
                    var (directionDegree, stops) = LinearGradientParser.ParseContent(str.AsSpan()[15..^1]);
                    return new Brush(directionDegree.ToRadian(), stops);
                }
                else {
                    var color = ExternalConstructor.Color4FromJson(source);
                    return new Brush(color);
                }
            }
            default: {
                source.ThrowInvalidFormat();
                return default;
            }
        }
    }

    public JsonValueKind ToJson(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("@type", typeof(Brush).FullName);
        writer.WriteEnum(nameof(Type), _type);
        switch(_type) {
            case BrushType.Solid: {
                writer.Write(nameof(SolidColor), _solidColor);
                break;
            }
            default: {
                throw new UnreachableException();
            }
        }
        writer.WriteEndObject();
        return JsonValueKind.Object;
    }
}
#endif
